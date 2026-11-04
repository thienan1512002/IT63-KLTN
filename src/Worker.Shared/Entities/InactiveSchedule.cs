using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Worker.Shared.Entities;

/// <summary>
/// Shared entity for InactiveSchedule table, used by both Web and Worker projects.
/// Tracks scheduled inactive periods for users.
/// </summary>
[Table("InactiveSchedules")]
public class InactiveSchedule
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;

    public DateTime From { get; set; }
    public DateTime To { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Hangfire job ID for the deactivation job (fires at From date).
    /// Used to cancel the job if user manually overrides.
    /// </summary>
    [MaxLength(100)]
    public string? DeactivateJobId { get; set; }

    /// <summary>
    /// Hangfire job ID for the reactivation job (fires at To date).
    /// Used to cancel the job if user manually overrides.
    /// </summary>
    [MaxLength(100)]
    public string? ReactivateJobId { get; set; }
}
