namespace Web.Models.Dtos.Admin;

public class EmailLogRowDto
{
    public long Id { get; set; }
    public string Subject { get; set; }
    public string ToAddress { get; set; }
    public string CcAddresses { get; set; }
    public string NotificationType { get; set; }
    public string WorkflowAction { get; set; }
    public string Status { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? RetryAt { get; set; }
    public string LastError { get; set; }
    public List<EmailAttemptDto> Attempts { get; set; }
}

public class EmailAttemptDto
{
    public long Id { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
