using Worker.Shared.Dtos;

namespace Worker.Shared.Interfaces;

public interface IEmailJob
{
    /// <summary>
    /// Creates EmailMasterData from request, then enqueues SendAsync as a separate retryable job.
    /// Called by Web app via Hangfire. Should NOT auto-retry (Attempts = 0).
    /// </summary>
    Task ProcessEmailAsync(EmailRequest request);

    /// <summary>
    /// SMTP send. Retry-able by Hangfire auto-retry and admin manual retry.
    /// </summary>
    Task SendAsync(long masterDataId);
}
