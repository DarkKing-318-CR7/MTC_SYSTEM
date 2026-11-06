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

        var now = DateTimeOffset.UtcNow;
        var currentTime = now.TimeOfDay;

        // Find broadcasts that are approved ("Đã duyệt" or "Đang phát")
        var activeBroadcasts = await context.Broadcasts
            .AsNoTracking()
            .Include(b => b.Schedules)
                .ThenInclude(s => s.TimeSlots)
            .Where(b => !b.IsDeleted && (b.Status == "Đã duyệt" || b.Status == "Đang phát"))
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var broadcast in activeBroadcasts)
        {
            foreach (var schedule in broadcast.Schedules.Where(s => !s.IsDeleted))
            {
                // 1. Kiểm tra dải ngày
                if (now < schedule.StartDate || now > schedule.EndDate)
                    continue;

                // 2. Kiểm tra chu kỳ lặp
                if (!IsDateMatchingRepeatRule(schedule, now))
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
                            now,
                            cancellationToken);

                        if (dispatched > 0) count++;
                    }
                }
                else if (schedule.TimeSlots.Count == 0)
                {
                    // Lịch phát không có time slot riêng lẻ -> phát trong suốt ngày
                    var dispatched = await dispatcher.DispatchBroadcastToDevicesAsync(
                        broadcast.Id,
                        schedule.Id,
                        null,
                        now,
                        cancellationToken);

                    if (dispatched > 0) count++;
                }
            }
        }

        return count;
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
