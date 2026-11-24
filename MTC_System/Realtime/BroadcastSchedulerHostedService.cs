using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Models;
using MTC_System.Services;

namespace MTC_System.Realtime;

/// <summary>
/// Tác vụ nền chạy định kỳ mỗi 1 phút:
/// - Quét và tự động suy ra trạng thái theo thời gian của bản tin:
///   + "Sắp tới lịch": khi còn <= 15 phút nữa đến giờ phát
///   + "Đang phát": khi trong khung giờ phát (gửi lệnh realtime dispatch tới thiết bị)
///   + "Chờ phát": đã phát xong ca hiện tại, chờ lịch tiếp theo (> 15 phút)
///   + "Đã kết thúc": hết toàn bộ lịch phát
/// - Chỉ quét các bản tin đã phê duyệt ("Chưa phát", "Đã duyệt", "Sắp tới lịch", "Đang phát", "Chờ phát").
///   Không can thiệp vào "Chờ duyệt", "Từ chối duyệt", "Đã hủy", "Tạm ngưng".
/// </summary>
public sealed class BroadcastSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastSchedulerHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public BroadcastSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BroadcastSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastSchedulerHostedService started with interval: {Interval}.", _checkInterval);

        // Chạy quét ngay 1 lần khi ứng dụng khởi động
        try
        {
            await CheckAndDispatchActiveBroadcastsAsync(stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error occurred during initial broadcast scheduling check.");
        }

        using var timer = new PeriodicTimer(_checkInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckAndDispatchActiveBroadcastsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during broadcast scheduling periodic check.");
            }
        }
    }

    public async Task<int> CheckAndDispatchActiveBroadcastsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IBroadcastRealtimeDispatcher>();

        // 1. Lấy cài đặt múi giờ của các tổ chức (mặc định "SE Asia Standard Time" - UTC+7)
        var orgTimezones = await context.SystemSettings
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.SettingKey == "TIMEZONE")
            .ToDictionaryAsync(s => s.OrganizationId, s => s.SettingValue, cancellationToken);

        // 2. Chỉ truy vấn các bản tin đang ở trạng thái hợp lệ để quét lập lịch
        var eligibleStatuses = new[] { "Chưa phát", "Đã duyệt", "Sắp tới lịch", "Đang phát", "Chờ phát" };

        var candidateBroadcasts = await context.Broadcasts
            .AsNoTracking()
            .Include(b => b.Schedules)
                .ThenInclude(s => s.TimeSlots)
            .Where(b => !b.IsDeleted && eligibleStatuses.Contains(b.Status))
            .ToListAsync(cancellationToken);

        int dispatchCount = 0;
        var activeScheduleIds = new HashSet<Guid>();
        var statusTransitions = new Dictionary<Guid, string>();

        foreach (var broadcast in candidateBroadcasts)
        {
            var tzString = orgTimezones.TryGetValue(broadcast.OrganizationId, out var tz) ? tz : "SE Asia Standard Time";
            var orgTimeZone = ResolveTimeZone(tzString);

            // Giờ hiện tại theo múi giờ kinh doanh của cơ sở (UTC+7)
            var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, orgTimeZone);
            var currentDate = localNow.Date;
            var currentTime = localNow.TimeOfDay;

            var nonDeletedSchedules = broadcast.Schedules.Where(s => !s.IsDeleted).ToList();
            if (nonDeletedSchedules.Count == 0)
            {
                continue;
            }

            bool isCurrentlyPlaying = false;

            // Kiểm tra các ca phát đang diễn ra ngay thời điểm hiện tại
            foreach (var schedule in nonDeletedSchedules)
            {
                var localStartDate = TimeZoneInfo.ConvertTime(schedule.StartDate, orgTimeZone);
                var localEndDate = TimeZoneInfo.ConvertTime(schedule.EndDate, orgTimeZone);

                bool isTodayInRange = currentDate >= localStartDate.Date && currentDate <= localEndDate.Date;
                bool isTodayMatch = isTodayInRange && BroadcastService.IsDateMatchingRepeatRule(schedule.RepeatType, schedule.RepeatDays, localNow);

                var yesterday = currentDate.AddDays(-1);
                var yesterdayOffset = new DateTimeOffset(yesterday, orgTimeZone.GetUtcOffset(yesterday));
                bool isYesterdayInRange = yesterday >= localStartDate.Date && yesterday <= localEndDate.Date;
                bool isYesterdayMatch = isYesterdayInRange && BroadcastService.IsDateMatchingRepeatRule(schedule.RepeatType, schedule.RepeatDays, yesterdayOffset);

                var slots = schedule.TimeSlots.Where(t => !t.IsDeleted).ToList();

                if (slots.Count > 0)
                {
                    foreach (var slot in slots)
                    {
                        if (IsSlotActiveNow(slot, currentTime, isTodayMatch, isYesterdayMatch))
                        {
                            isCurrentlyPlaying = true;
                            activeScheduleIds.Add(schedule.Id);

                            var dispatched = await dispatcher.DispatchBroadcastToDevicesAsync(
                                broadcast.Id,
                                schedule.Id,
                                slot.Id,
                                localNow,
                                cancellationToken);

                            if (dispatched > 0)
                            {
                                dispatchCount++;
                            }
                        }
                    }
                }
                else
                {
                    // Lịch phát không có time slot riêng lẻ -> phát trong suốt ngày
                    if (isTodayMatch && localNow >= localStartDate && localNow <= localEndDate)
                    {
                        isCurrentlyPlaying = true;
                        activeScheduleIds.Add(schedule.Id);

                        var dispatched = await dispatcher.DispatchBroadcastToDevicesAsync(
                            broadcast.Id,
                            schedule.Id,
                            null,
                            localNow,
                            cancellationToken);

                        if (dispatched > 0)
                        {
                            dispatchCount++;
                        }
                    }
                }
            }

            // Suy ra trạng thái mục tiêu dựa trên thời gian
            string targetStatus;

            if (isCurrentlyPlaying)
            {
                targetStatus = "Đang phát";
            }
            else
            {
                var earliestNext = FindEarliestNextOccurrence(nonDeletedSchedules, localNow, orgTimeZone);

                if (earliestNext == null)
                {
                    // Hết toàn bộ lịch phát
                    targetStatus = "Đã kết thúc";
                }
                else
                {
                    var timeUntilNext = earliestNext.Value - localNow;
                    if (timeUntilNext <= TimeSpan.FromMinutes(15))
                    {
                        // Còn <= 15 phút nữa đến giờ phát -> Sắp tới lịch
                        targetStatus = "Sắp tới lịch";
                    }
                    else
                    {
                        // Còn > 15 phút:
                        // Nếu đã từng phát ca trước đó -> Chờ phát (đã phát xong ca hiện tại, chờ lịch tiếp theo)
                        // Nếu chưa từng phát ca nào -> Giữ "Chưa phát"
                        bool hadPast = HasPastOccurrence(broadcast, nonDeletedSchedules, localNow, orgTimeZone);
                        targetStatus = hadPast ? "Chờ phát" : "Chưa phát";
                    }
                }
            }

            if (broadcast.Status != targetStatus)
            {
                statusTransitions[broadcast.Id] = targetStatus;
            }
        }

        // Cập nhật trạng thái bản tin trong cơ sở dữ liệu
        if (statusTransitions.Count > 0)
        {
            var idsToUpdate = statusTransitions.Keys.ToList();
            var broadcastsToUpdate = await context.Broadcasts
                .Where(b => idsToUpdate.Contains(b.Id))
                .ToListAsync(cancellationToken);

            foreach (var b in broadcastsToUpdate)
            {
                if (statusTransitions.TryGetValue(b.Id, out var newStatus) && b.Status != newStatus)
                {
                    _logger.LogInformation(
                        "Broadcast {BroadcastCode} ({BroadcastId}) status changed: {OldStatus} -> {NewStatus}",
                        b.BroadcastCode, b.Id, b.Status, newStatus);

                    b.Status = newStatus;
                    b.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        // Dừng phát các thiết bị đang phát bản tin không còn nằm trong lịch active
        await dispatcher.StopBroadcastsOutsideActiveSchedulesAsync(activeScheduleIds, cancellationToken);

        return dispatchCount;
    }

    private static bool IsSlotActiveNow(
        BroadcastTimeSlot slot,
        TimeSpan currentTime,
        bool isTodayMatch,
        bool isYesterdayMatch)
    {
        if (slot.StartTime <= slot.EndTime)
        {
            // Khung giờ thông thường trong ngày
            return isTodayMatch && currentTime >= slot.StartTime && currentTime <= slot.EndTime;
        }
        else
        {
            // Khung giờ qua đêm (ví dụ: 23:00 - 01:00 hôm sau)
            return (isTodayMatch && currentTime >= slot.StartTime) ||
                   (isYesterdayMatch && currentTime <= slot.EndTime);
        }
    }

    private static DateTimeOffset? FindEarliestNextOccurrence(
        List<BroadcastSchedule> schedules,
        DateTimeOffset localNow,
        TimeZoneInfo orgTimeZone)
    {
        DateTimeOffset? earliest = null;
        var currentDate = localNow.Date;
        var currentTime = localNow.TimeOfDay;

        foreach (var schedule in schedules)
        {
            var localStartDate = TimeZoneInfo.ConvertTime(schedule.StartDate, orgTimeZone);
            var localEndDate = TimeZoneInfo.ConvertTime(schedule.EndDate, orgTimeZone);

            if (localNow > localEndDate)
                continue; // Lịch này đã hết hạn trong quá khứ

            var slots = schedule.TimeSlots.Where(t => !t.IsDeleted).ToList();

            // 1. Kiểm tra các slot còn lại trong ngày hôm nay
            if (currentDate >= localStartDate.Date && currentDate <= localEndDate.Date &&
                BroadcastService.IsDateMatchingRepeatRule(schedule.RepeatType, schedule.RepeatDays, localNow))
            {
                if (slots.Count == 0)
                {
                    if (localStartDate > localNow)
                    {
                        if (earliest == null || localStartDate < earliest)
                            earliest = localStartDate;
                    }
                }
                else
                {
                    foreach (var slot in slots)
                    {
                        if (slot.StartTime > currentTime)
                        {
                            var slotStart = new DateTimeOffset(currentDate.Add(slot.StartTime), orgTimeZone.GetUtcOffset(currentDate.Add(slot.StartTime)));
                            if (earliest == null || slotStart < earliest)
                                earliest = slotStart;
                        }
                    }
                }
            }

            // 2. Tìm ca phát sớm nhất ở các ngày tiếp theo trong tương lai
            var searchStartDay = currentDate.AddDays(1);
            if (searchStartDay < localStartDate.Date)
                searchStartDay = localStartDate.Date;

            var maxSearchDay = localEndDate.Date;
            var capDay = currentDate.AddDays(366);
            if (maxSearchDay > capDay)
                maxSearchDay = capDay;

            for (var d = searchStartDay; d <= maxSearchDay; d = d.AddDays(1))
            {
                var dayOffset = new DateTimeOffset(d, orgTimeZone.GetUtcOffset(d));
                if (!BroadcastService.IsDateMatchingRepeatRule(schedule.RepeatType, schedule.RepeatDays, dayOffset))
                    continue;

                if (slots.Count == 0)
                {
                    var dayStart = d < localStartDate.Date ? localStartDate : dayOffset;
                    if (earliest == null || dayStart < earliest)
                        earliest = dayStart;
                    break;
                }
                else
                {
                    var minSlot = slots.OrderBy(s => s.StartTime).First();
                    var slotStart = new DateTimeOffset(d.Add(minSlot.StartTime), orgTimeZone.GetUtcOffset(d.Add(minSlot.StartTime)));
                    if (earliest == null || slotStart < earliest)
                        earliest = slotStart;
                    break;
                }
            }
        }

        return earliest;
    }

    private static bool HasPastOccurrence(
        Broadcast broadcast,
        List<BroadcastSchedule> schedules,
        DateTimeOffset localNow,
        TimeZoneInfo orgTimeZone)
    {
        if (broadcast.Status == "Đang phát" || broadcast.Status == "Chờ phát")
            return true;

        var currentDate = localNow.Date;
        var currentTime = localNow.TimeOfDay;

        foreach (var schedule in schedules)
        {
            var localStartDate = TimeZoneInfo.ConvertTime(schedule.StartDate, orgTimeZone);
            if (localStartDate >= localNow)
                continue;

            var localEndDate = TimeZoneInfo.ConvertTime(schedule.EndDate, orgTimeZone);
            var slots = schedule.TimeSlots.Where(t => !t.IsDeleted).ToList();

            // Kiểm tra các ngày trước ngày hiện tại
            var checkDate = localStartDate.Date;
            var pastDaysLimit = currentDate.AddDays(-1);
            if (pastDaysLimit > localEndDate.Date)
                pastDaysLimit = localEndDate.Date;

            for (var d = checkDate; d <= pastDaysLimit; d = d.AddDays(1))
            {
                var dayOffset = new DateTimeOffset(d, orgTimeZone.GetUtcOffset(d));
                if (BroadcastService.IsDateMatchingRepeatRule(schedule.RepeatType, schedule.RepeatDays, dayOffset))
                {
                    return true;
                }
            }

            // Kiểm tra ngày hôm nay: có slot nào đã kết thúc trước currentTime
            if (currentDate >= localStartDate.Date && currentDate <= localEndDate.Date &&
                BroadcastService.IsDateMatchingRepeatRule(schedule.RepeatType, schedule.RepeatDays, localNow))
            {
                if (slots.Count == 0)
                {
                    if (localStartDate < localNow)
                        return true;
                }
                else
                {
                    foreach (var slot in slots)
                    {
                        if (slot.StartTime <= slot.EndTime && slot.EndTime < currentTime)
                            return true;
                        if (slot.StartTime > slot.EndTime && slot.EndTime < currentTime)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    private static TimeZoneInfo ResolveTimeZone(string? tzString)
    {
        if (string.IsNullOrWhiteSpace(tzString))
            tzString = "SE Asia Standard Time";

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(tzString);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                if (tzString.Equals("SE Asia Standard Time", StringComparison.OrdinalIgnoreCase) ||
                    tzString.Contains("07:00") || tzString.Contains("Bangkok") || tzString.Contains("Hanoi") || tzString.Contains("Vietnam"))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
                return TimeZoneInfo.CreateCustomTimeZone("UTC+07:00", TimeSpan.FromHours(7), "SE Asia Standard Time", "SE Asia Standard Time");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("UTC+07:00", TimeSpan.FromHours(7), "SE Asia Standard Time", "SE Asia Standard Time");
            }
        }
    }
}
