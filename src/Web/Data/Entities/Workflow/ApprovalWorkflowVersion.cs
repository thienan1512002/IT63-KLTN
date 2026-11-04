using System.ComponentModel.DataAnnotations;

namespace Web.Data.Entities.Workflow;

public class ApprovalWorkflowVersion
{
    [MaxLength(50)] public string ApprovalCaseId { get; set; } = string.Empty;
    public ApprovalCase ApprovalCase { get; set; } = null!;
    public int Revision { get; set; }
    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string? Description { get; set; }
    public DateTime PublishedAt { get; set; }
    [MaxLength(256)] public string? PublishedBy { get; set; }
}
