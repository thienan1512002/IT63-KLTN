using System.ComponentModel.DataAnnotations;
using Web.Data.Enums.Workflow;

namespace Web.Models.Dtos.Workflow;

public class RequestTypeSaveDto
{
    [Required, MaxLength(30)] public string TemplateCode { get; set; } = "INTERNAL";
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string ApprovalCaseId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool RequireRequestedDate { get; set; } = true;
    public bool RequireAmount { get; set; }
    public bool RequireReason { get; set; }
    public bool AllowAttachment { get; set; } = true;
}

public class WorkflowSaveDto
{
    [Required, MaxLength(50)] public string CaseId { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    [Range(1, int.MaxValue)] public int Revision { get; set; } = 1;
    [MinLength(1), MaxLength(100)] public List<WorkflowStepSaveDto> Steps { get; set; } = [];
}

public class WorkflowStepSaveDto
{
    [Range(1, 100)] public int StepOrder { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public ApproverKind ApproverKind { get; set; }
    [MaxLength(256)] public string ApproverUserName { get; set; }
    [MaxLength(256)] public string ApproverRoleName { get; set; }
    public int? ApproverDepartmentId { get; set; }
}
