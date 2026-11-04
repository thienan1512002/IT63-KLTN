

using Worker.Shared.Enums;

namespace Worker.Shared.Entities;

/// <summary>
/// Master data for an email. Created once before enqueuing, never duplicated on retry.
/// </summary>
public class EmailMasterData
{
    public long Id { get; set; }
    public NotificationType? NotificationType { get; set; }
    public WorkflowAction? WorkflowAction { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? TemplateDataJson { get; set; }
    public EmailMasterDataStatus Status { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime? RetryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public ICollection<EmailLog> Logs { get; set; } = [];
}
