namespace Web.Services.Workflow.Core;

/// <summary>
/// Dispatches in-app notifications and enqueues workflow emails for each workflow event.
/// </summary>
public interface IApprovalNotifier
{
    /// <summary>Notify the requester that their request has been submitted.</summary>
    Task NotifySubmittedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null);

    /// <summary>
    /// Notify the next approver that action is required, or the requester if the request is completed.
    /// Set <paramref name="firstStep"/> to true when notifying immediately after creation (no prior approval step).
    /// </summary>
    Task NotifyApprovedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null);

    /// <summary>
    /// Notify the requester (and CC previous approvers) that the request was returned or rejected.
    /// </summary>
    Task NotifyReturnedOrRejectedAsync(ApprovalActionContext context, bool isRejection = false, Dictionary<string, string> additionalData = null);

    /// <summary>Dismiss unread in-app toast notifications for the given request URL.</summary>
    Task DismissNotificationsAsync(string faxCode, string url);
}
