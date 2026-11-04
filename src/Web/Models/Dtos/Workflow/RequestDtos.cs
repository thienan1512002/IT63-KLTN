using System.ComponentModel.DataAnnotations;
using Web.Data.Enums.Workflow;

namespace Web.Models.Dtos.Workflow;

public class RequestEditDto
{
    public RequestBusinessDataDto BusinessData { get; set; } = new();
    public int? Id { get; set; }
    [Required]
    public int RequestTypeId { get; set; }
    [Required, MaxLength(250)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
    public DateTime? RequestedDate { get; set; }
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;
    [Range(0, 9999999999999999.99)]
    public decimal? Amount { get; set; }
    [MaxLength(2000)]
    public string? Reason { get; set; }
}

public class RequestActionDto
{
    [MaxLength(2000)]
    public string? Comment { get; set; }
}

public class RequestListItemDto
{
    public int Id { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public RequestPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? CurrentStepOrder { get; set; }
    public string CurrentApprover { get; set; } = string.Empty;
}

public class RequestDetailsDto : RequestListItemDto
{
    public string TemplateCode { get; set; }
    public RequestBusinessDataDto BusinessData { get; set; }
    public List<RequestBusinessDisplayField> BusinessFields { get; set; } = [];
    public List<RequestLineDisplayDto> BusinessItems { get; set; } = [];
    public List<string> ParticipantNames { get; set; } = [];
    public int RequestTypeId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime? RequestedDate { get; set; }
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
    public bool CanApprove { get; set; }
    public List<RequestApprovalStepDto> Steps { get; set; } = [];
    public List<RequestHistoryDto> Histories { get; set; } = [];
    public List<RequestAttachmentDto> Attachments { get; set; } = [];
}

public class RequestApprovalStepDto
{
    public int Id { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public ApprovalStepStatus Status { get; set; }
    public string? Comment { get; set; }
    public DateTime? ActionAt { get; set; }
}

public class RequestHistoryDto
{
    public long Id { get; set; }
    public ApprovalAction Action { get; set; }
    public string ActionBy { get; set; } = string.Empty;
    public int? StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public RequestStatus PreviousStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public DateTime ActionAt { get; set; }
}

public class RequestAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
