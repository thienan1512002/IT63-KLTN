using System.Text.Json;
using Worker.Shared.Entities;
using Worker.Shared.Enums;

namespace Worker.Services.Handlers;

public class UserStatusEmailHandler(
    IEmailTemplateService templateService) : INotificationEmailHandler
{
    public NotificationType NotificationType => NotificationType.UserStatus;

    public string BuildBody(EmailMasterData masterData)
    {
        var template = templateService.LoadTemplate(Path.Combine("Account", "InactiveAccount.html"));

        var templateData = !string.IsNullOrEmpty(masterData.TemplateDataJson)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(masterData.TemplateDataJson) ?? []
            : new Dictionary<string, string>();

        return templateService.ApplyTemplateData(template, templateData);
    }
}
