using Worker.Shared.Enums;

namespace Worker.Shared.Dtos;

public class EmailRequest
{
    public string ToAddress { get; set; } = string.Empty;
    public List<string> CcAddresses { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType? NotificationType { get; set; }

    //<summary>
    /// Optional workflow action for emails related to workflow notifications.
    /// Used by WorkflowEmailHandler to determine template and default subject.
    /// </summary>
    public WorkflowAction? WorkflowAction { get; set; }
    public Dictionary<string, string> TemplateData { get; set; } = [];
}
