using System.ComponentModel.DataAnnotations;

namespace Web.Data.Entities.Workflow;

public class RequestType
{
    public int Id { get; set; }
    [Required, MaxLength(30)]
    public string Code { get; set; } = string.Empty;
    [Required, MaxLength(30)]
    public string TemplateCode { get; set; } = "INTERNAL";
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool RequireRequestedDate { get; set; } = true;
    public bool RequireAmount { get; set; }
    public bool RequireReason { get; set; }
    public bool AllowAttachment { get; set; } = true;
    [Required, MaxLength(50)]
    public string ApprovalCaseId { get; set; } = string.Empty;
    public ApprovalCase ApprovalCase { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
