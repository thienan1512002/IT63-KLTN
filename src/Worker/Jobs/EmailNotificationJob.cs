using Hangfire;
using Worker.Services;
using Worker.Shared.Dtos;
using Worker.Shared.Interfaces;

namespace Worker.Jobs;

public class EmailNotificationJob(
    IEmailSender emailSender,
    ILogger<EmailNotificationJob> logger) : IEmailJob
{
    /// <summary>
    /// Creates master data then enqueues the retryable send job.
    /// No auto-retry to avoid duplicate master data on failure.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessEmailAsync(EmailRequest request)
    {
        logger.LogInformation("Creating email master data [To={To}, Subject={Subject}]",
            request.ToAddress, request.Subject);

        var masterDataId = await emailSender.CreateMasterDataAsync(request);

        BackgroundJob.Enqueue<IEmailJob>(x => x.SendAsync(masterDataId));

        logger.LogInformation("Email send job enqueued [MasterDataId={Id}]", masterDataId);
    }

    /// <summary>
    /// Loads master data, rebuilds body, sends via SMTP.
    /// Hangfire auto-retries 3 times. Admin can also manually retry via this method.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendAsync(long masterDataId)
    {
        logger.LogInformation("Sending email [MasterDataId={Id}]", masterDataId);
        await emailSender.SendAsync(masterDataId);
    }
}
