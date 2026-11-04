using Hangfire;
using Worker.Services;

namespace Worker.Jobs;

public class EmailReminderJob(
    IEmailSender emailSender,
    ILogger<EmailReminderJob> logger)
{
    /// <summary>
    /// SMTP send. Retry-able by Hangfire and admin manual retry.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendAsync(long masterDataId)
    {
        logger.LogInformation("Sending reminder email [MasterDataId={Id}]", masterDataId);
        await emailSender.SendAsync(masterDataId);
    }
}
