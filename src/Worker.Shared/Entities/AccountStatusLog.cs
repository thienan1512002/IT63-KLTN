using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Worker.Shared.Enums;

namespace Worker.Shared.Entities;

/// <summary>
/// Shared entity for AccountStatusLog table, used by both Web and Worker projects.
/// Tracks all account status changes with timestamps and reasons.
/// </summary>
[Table("AccountStatusLogs")]
public class AccountStatusLog
{
    public long Id { get; set; }

    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    public AccountStatus NewStatus { get; set; }

    public StatusChangeReason Reason { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.Now;

    [MaxLength(1000)]
    public string? Comment { get; set; }
}
