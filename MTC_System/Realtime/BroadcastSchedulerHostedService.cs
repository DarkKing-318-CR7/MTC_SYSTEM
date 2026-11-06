using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Models;

namespace MTC_System.Realtime;

public sealed class BroadcastSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastSchedulerHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public BroadcastSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BroadcastSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastSchedulerHostedService started.");

        using var timer = new PeriodicTimer(_checkInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckAndDispatchActiveBroadcastsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during broadcast scheduling check.");
            }
        }
    }

    public async Task<int> CheckAndDispatchActiveBroadcastsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IBroadcastRealtimeDispatcher>();

        // P1 – Query organization timezone settings (fallback "SE Asia Standard Time")
        var orgTimezones = await context.SystemSettings
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.SettingKey == "TIMEZONE")
            .ToDictionaryAsync(s => s.OrganizationId, s => s.SettingValue, cancellationToken);

        // P0 – Find broadcasts that are approved ("Chưa phát", "Đã duyệt", or "Đang phát")
        var activeBroadcasts = await context.Broadcasts
            .AsNoTracking()
            .Include(b => b.Schedules)
                .ThenInclude(s => s.TimeSlots)
            .Where(b => !b.IsDeleted && (b.Status == "Chưa phát" || b.Status == "Đã duyệt" || b.Status == "Đang phát"))
            .ToListAsync(cancellationToken);

        int count = 0;
        var broadcastsToUpdateStatus = new HashSet<Guid>();

        foreach (var broadcast in activeBroadcasts)
        {
            var tzString = orgTimezones.TryGetValue(broadcast.OrganizationId, out var tz) ? tz : "SE Asia Standard Time";
            var orgTimeZone = ResolveTimeZone(tzString);

            // Compute current time in the organization's business timezone (e.g. UTC+7)
            var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, orgTimeZone);
            var currentTime = localNow.TimeOfDay;

            foreach (var schedule in broadcast.Schedules.Where(s => !s.IsDeleted))
            {
                // 1. Kiểm tra dải ngày theo múi giờ địa phương
                var localStartDate = TimeZoneInfo.ConvertTime(schedule.StartDate, orgTimeZone);
                var localEndDate = TimeZoneInfo.ConvertTime(schedule.EndDate, orgTimeZone);
                if (localNow < localStartDate || localNow > localEndDate)
                    continue;

                // 2. Kiểm tra chu kỳ lặp
                if (!IsDateMatchingRepeatRule(schedule, localNow))
                    continue;

                // 3. Kiểm tra khung giờ (TimeSlots)
                var activeSlots = schedule.TimeSlots
                    .Where(t => !t.IsDeleted && t.StartTime <= currentTime && currentTime <= t.EndTime)
                    .ToList();

                if (activeSlots.Count > 0)
                {
                    foreach (var slot in activeSlots)
                    {
                        var dispatched = await dispatcher.DispatchBroadcastToDevicesAsync(
                            broadcast.Id,
                            schedule.Id,
                            slot.Id,
                            localNow,
                            cancellationToken);

                        if (dispatched > 0)
                        {
                            count++;
                            if (broadcast.Status == "Chưa phát" || broadcast.Status == "Đã duyệt")
                            {
                                broadcastsToUpdateStatus.Add(broadcast.Id);
                            }
                        }
                    }
                }
                else if (schedule.TimeSlots.Count == 0)
                {
                    // Lịch phát không có time slot riêng lẻ -> phát trong suốt ngày
                    var dispatched = await dispatcher.DispatchBroadcastToDevicesAsync(
                        broadcast.Id,
                        schedule.Id,
                        null,
                        localNow,
                        cancellationToken);

                    if (dispatched > 0)
                    {
                        count++;
                        if (broadcast.Status == "Chưa phát" || broadcast.Status == "Đã duyệt")
                        {
                            broadcastsToUpdateStatus.Add(broadcast.Id);
                        }
                    }
                }
            }
        }

        // Cập nhật trạng thái bản tin sang "Đang phát" khi đã bắn lệnh điều phối thành công
        if (broadcastsToUpdateStatus.Count > 0)
        {
            var broadcastsToUpdate = await context.Broadcasts
                .Where(b => broadcastsToUpdateStatus.Contains(b.Id))
                .ToListAsync(cancellationToken);

            foreach (var b in broadcastsToUpdate)
            {
                if (b.Status != "Đang phát")
                {
                    b.Status = "Đang phát";
                    b.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        return count;
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

    private static bool IsDateMatchingRepeatRule(BroadcastSchedule schedule, DateTimeOffset now)
    {
        var repeatType = schedule.RepeatType?.Trim() ?? "Không lặp";

        if (repeatType.Equals("Không lặp", StringComparison.OrdinalIgnoreCase) ||
            repeatType.Equals("Hàng ngày", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (repeatType.Equals("Theo tuần", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(schedule.RepeatDays))
                return true;

            var dayOfWeek = now.DayOfWeek;
            var vnDayCode = dayOfWeek switch
            {
                DayOfWeek.Monday => "2",
                DayOfWeek.Tuesday => "3",
                DayOfWeek.Wednesday => "4",
                DayOfWeek.Thursday => "5",
                DayOfWeek.Friday => "6",
                DayOfWeek.Saturday => "7",
                DayOfWeek.Sunday => "CN",
                _ => ""
            };

            var days = schedule.RepeatDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return days.Contains(vnDayCode, StringComparer.OrdinalIgnoreCase) ||
                   days.Contains(dayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        if (repeatType.Equals("Theo tháng", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(schedule.RepeatDays))
                return true;

            var dayOfMonth = now.Day.ToString();
            var days = schedule.RepeatDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return days.Contains(dayOfMonth);
        }

        return true;
    }
}
