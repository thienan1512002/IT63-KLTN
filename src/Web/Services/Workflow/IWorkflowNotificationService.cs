using Web.Models.Dtos.Notification;
using Worker.Shared.Enums;

namespace Web.Services.Workflow;

public interface IWorkflowNotificationService
{
    /// <summary>
    /// Creates in-app notifications (NotiToast) for the given workflow action.
    /// </summary>
    Task SaveNotificationsAsync(WorkflowAction action, EmailContent content);

    /// <summary>
    /// Builds an EmailRequest and enqueues it via Hangfire for the Worker to send.
    /// </summary>
    Task EnqueueWorkflowEmailAsync(WorkflowAction action, EmailContent content, string currentApprover = null);

    /// <summary>
    /// Combines the above two methods: creates in-app notifications and enqueues email sending for the given workflow action.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="content"></param>
    /// <param name="currentApprover">User.Identity.Name of the current approver</param>
    /// <returns></returns>
    Task SendMailAndNotificationAsync(WorkflowAction action, EmailContent content, string currentApprover = null);
}
