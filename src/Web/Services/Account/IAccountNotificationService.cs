namespace Web.Services.Account;

public interface IAccountNotificationService
{
    /// <summary>
    /// Sends an email + NotiToast to all backup approvers when a main approver goes inactive.
    /// </summary>
    Task SendInactiveNotificationAsync(string mainApproverUserName, DateTime from, DateTime to, string comment);

    /// <summary>
    /// Sends a password-reset email to the specified address.
    /// </summary>
    Task SendResetPasswordEmailAsync(string toAddress, string recipientName, string resetLink, string refreshLink);
}
