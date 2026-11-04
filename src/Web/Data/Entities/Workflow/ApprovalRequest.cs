using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Web.Data.Entities.Admin;
using Web.Data.Entities.Identity;
using Web.Data.Enums.Workflow;

namespace Web.Data.Entities.Workflow;

public class ApprovalRequest
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string ApplicationNo { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? DescriptionNo { get; set; }
    public DateTime SubmitDate { get; set; }
    [Required, MaxLength(400)]
    public string RequestName { get; set; } = string.Empty;
    public bool UrgencyLevel { get; set; }
    [Required, MaxLength(200)]
    public string FaxCode { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? SiteName { get; set; }
    public int? SiteId { get; set; }
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string SectionName { get; set; } = string.Empty;
    public int SectionId { get; set; }
    [Required, MaxLength(400)]
    public string DeptName { get; set; } = string.Empty;
    public int DeptId { get; set; }
    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Position { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? PhoneNumber { get; set; }
    public int Process { get; set; }
    public int TotalStep { get; set; }
    [Required, MaxLength(200)]
    public string MainApproval { get; set; } = string.Empty;
    public bool Complete { get; set; }
    public ApprovalType CurrentApprovalType { get; set; }
    [Required, MaxLength(100)]
    public string FormCode { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? ApprovalCaseId { get; set; }

    public int? WorkflowRevision { get; set; }
    public DateTime? OperationsSyncedAt { get; set; }
    public ApprovalCase? ApprovalCase { get; set; }
    public bool IsRemoved { get; set; }
    public ICollection<ApprovalDetail> ApprovalDetails { get; set; } = [];

    // Generic request fields layered on the proven Kimsen workflow engine.
    public int RequestTypeId { get; set; }
    public RequestType RequestType { get; set; } = null!;
    [Required, MaxLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;
    public AppUser CreatedByUser { get; set; } = null!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    [Required, MaxLength(250)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
    public DateTime? RequestedDate { get; set; }
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;
    public decimal? Amount { get; set; }
    [MaxLength(2000)]
    public string? Reason { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Draft;
    public int? CurrentStepOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<RequestAttachment> Attachments { get; set; } = [];
    public ICollection<ApprovalHistory> Histories { get; set; } = [];
    public RequestBusinessData BusinessData { get; set; }
}
