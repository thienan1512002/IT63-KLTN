namespace Worker.Shared.Interfaces;

/// <summary>
/// Interface for user status Hangfire jobs.
/// Shared between Web (to schedule) and Worker (to execute).
/// </summary>
public interface IUserStatusJob
{
    /// <summary>
    /// Deactivates a user based on an InactiveSchedule record.
    /// Fires at the schedule's "From" date.
    /// </summary>
    Task DeactivateUserAsync(int scheduleId);

    /// <summary>
    /// Reactivates a user based on an InactiveSchedule record.
    /// Fires at the schedule's "To" date.
    /// </summary>
    Task ReactivateUserAsync(int scheduleId);
}
