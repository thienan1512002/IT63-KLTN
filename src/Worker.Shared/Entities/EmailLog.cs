namespace Worker.Shared.Entities;

/// <summary>
/// Each send attempt (initial + retries) creates one EmailLog record.
/// Logs both success and failure.
/// </summary>
public class EmailLog
{
    public long Id { get; set; }
    public long EmailMasterDataId { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    public EmailMasterData EmailMasterData { get; set; } = null!;
}
