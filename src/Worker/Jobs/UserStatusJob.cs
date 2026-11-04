using Hangfire;
using Microsoft.EntityFrameworkCore;
using Worker.Data;
using Worker.Shared.Data;
using Worker.Shared.Entities;
using Worker.Shared.Enums;
using Worker.Shared.Interfaces;

namespace Worker.Jobs;

/// <summary>
/// Hangfire job for processing user inactive schedules.
/// Each method handles a single schedule by ID — event-driven, not polling.
/// Scheduled by Web app when user creates/cancels an inactive schedule.
/// </summary>
public class UserStatusJob(
    WebDbContext webDb,
    ILogger<UserStatusJob> logger) : IUserStatusJob
{
    /// <summary>
    /// Fires at the schedule's "From" date.
    /// Sets the user offline and marks the schedule as processed.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task DeactivateUserAsync(int scheduleId)
    {
        var schedule = await webDb.InactiveSchedules.FindAsync(scheduleId);
        if (schedule == null)
        {
            logger.LogWarning("DeactivateUser: Schedule {ScheduleId} not found, skipping.", scheduleId);
            return;
        }


        var user = await webDb.UserStatusRecords
            .FirstOrDefaultAsync(u => u.UserName == schedule.UserName);

        if (user == null)
        {
            logger.LogWarning("DeactivateUser: User {UserName} not found for schedule {ScheduleId}.", schedule.UserName, scheduleId);
            return;
        }

        // Set user offline
        user.IsOnline = false;

        // Log the deactivation
        var statusLog = new AccountStatusLog
        {
            Username = schedule.UserName,
            NewStatus = AccountStatus.Inactive,
            Reason = StatusChangeReason.ByScheduledDeactivation,
            Comment = $"Scheduled deactivation (schedule {schedule.Id}): {schedule.Comment}",
            ChangedAt = DateTime.Now
        };

        webDb.AccountStatusLogs.Add(statusLog);
    
        await webDb.SaveChangesAsync();
    }

    /// <summary>
    /// Fires at the schedule's "To" date.
    /// Sets the user back online and marks the schedule as removed (fully completed).
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ReactivateUserAsync(int scheduleId)
    {
        var schedule = await webDb.InactiveSchedules.FindAsync(scheduleId);
        if (schedule == null)
        {
            logger.LogWarning("ReactivateUser: Schedule {ScheduleId} not found, skipping.", scheduleId);
            return;
        }


        var user = await webDb.UserStatusRecords
            .FirstOrDefaultAsync(u => u.UserName == schedule.UserName);

        if (user == null)
        {
            logger.LogWarning("ReactivateUser: User {UserName} not found for schedule {ScheduleId}.", schedule.UserName, scheduleId);
            return;
        }

        // Set user online
        user.IsOnline = true;

        // Log the reactivation
        var statusLog = new AccountStatusLog
        {
            Username = schedule.UserName,
            NewStatus = AccountStatus.Active,
            Reason = StatusChangeReason.ByScheduledReactivation,
            Comment = $"Scheduled reactivation (schedule {schedule.Id}): {schedule.Comment}",
            ChangedAt = DateTime.Now
        };

        webDb.AccountStatusLogs.Add(statusLog);

        // Clean up completed schedule - hard delete instead of soft delete
        webDb.InactiveSchedules.Remove(schedule);
        await webDb.SaveChangesAsync();

    }
}
