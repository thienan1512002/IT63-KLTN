using System.ComponentModel.DataAnnotations;

namespace Web.Data.Entities.Workflow;

public class RequestAttachment
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public ApprovalRequest Request { get; set; } = null!;
    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;
    [MaxLength(150)]
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    [Required, MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
