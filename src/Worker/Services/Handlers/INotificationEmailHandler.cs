
using Worker.Shared.Entities;
using Worker.Shared.Enums;

namespace Worker.Services.Handlers;

/// <summary>
/// Each notification type implements this interface.
/// Pure body builder — no side effects, no SMTP.
/// </summary>
public interface INotificationEmailHandler
{
    NotificationType NotificationType { get; }

    /// <summary>
    /// Builds the HTML body from template + master data's TemplateDataJson.
    /// </summary>
    string BuildBody(EmailMasterData masterData);
}
