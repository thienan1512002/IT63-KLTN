using System.ComponentModel.DataAnnotations;
using Web.Data.Enums.Workflow;

namespace Web.Data.Entities.Workflow;

public class ApprovalHistory
{
    public long Id { get; set; }
    public int RequestId { get; set; }
    public ApprovalRequest Request { get; set; } = null!;
    public int? ApprovalDetailId { get; set; }
    public ApprovalDetail? ApprovalDetail { get; set; }
    [Required, MaxLength(50)]
    public string RequestCode { get; set; } = string.Empty;
    [Required, MaxLength(450)]
    public string ActionByUserId { get; set; } = string.Empty;
    public int? StepOrder { get; set; }
    public ApprovalAction Action { get; set; }
    [MaxLength(2000)]
    public string? Comment { get; set; }
    public RequestStatus PreviousStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}
