
using Worker.Shared.Entities;
using Worker.Shared.Enums;

namespace Worker.Services.Handlers;

/// <summary>
/// Routes body building to the correct handler based on NotificationType.
/// </summary>
public class NotificationEmailDispatcher(
    IEnumerable<INotificationEmailHandler> handlers,
    ILogger<NotificationEmailDispatcher> logger)
{
    private readonly Dictionary<NotificationType, INotificationEmailHandler> _handlerMap =
        handlers.ToDictionary(h => h.NotificationType);

    public string BuildBody(EmailMasterData masterData)
    {
        var notificationType = masterData.NotificationType
            ?? throw new ArgumentException("NotificationType is required");

        if (!_handlerMap.TryGetValue(notificationType, out var handler))
            throw new NotSupportedException($"No handler for NotificationType: {notificationType}");

        logger.LogInformation("Building email body [Type={NotificationType}, To={To}]",
            notificationType, masterData.ToAddress);

        return handler.BuildBody(masterData);
    }
}
