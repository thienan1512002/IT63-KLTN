namespace Web.Models.Dtos.Workflow;

public class RequestApprovalPreviewDto
{
    public string WorkflowName { get; set; } = string.Empty;
    public int? Revision { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSnapshot { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<RequestApprovalPreviewStepDto> Steps { get; set; } = [];
}

public class RequestApprovalPreviewStepDto
{
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Assignment { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public List<RequestApprovalPreviewPersonDto> Approvers { get; set; } = [];
}

public class RequestApprovalPreviewPersonDto
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
}
