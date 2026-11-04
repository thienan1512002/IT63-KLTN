using System.Text.Json;
using System.Net;
using Worker.Shared.Entities;
using Worker.Shared.Enums;

namespace Worker.Services.Handlers;

public class WorkflowEmailHandler(
    IEmailTemplateService templateService) : INotificationEmailHandler
{
    public NotificationType NotificationType => NotificationType.Workflow;

    public string BuildBody(EmailMasterData masterData)
    {
        var action = masterData.WorkflowAction?.ToString()
            ?? throw new ArgumentException("WorkflowAction is required for workflow emails");

        var templatePath = Path.Combine("Workflow", $"{action}.html");
        var template = templateService.LoadTemplate(templatePath);

        var templateData = !string.IsNullOrEmpty(masterData.TemplateDataJson)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(masterData.TemplateDataJson) ?? []
            : new Dictionary<string, string>();

        // Queued emails created before these fields were introduced must still render cleanly.
        templateData.TryAdd("RequestTitle", templateData.GetValueOrDefault("RequestName", ""));
        templateData.TryAdd("ApplicationNo", "—");
        return templateService.ApplyTemplateData(template,
            templateData.ToDictionary(x => x.Key, x => WebUtility.HtmlEncode(x.Value ?? "")));
    }
}
