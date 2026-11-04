using Worker.Shared.Dtos;

namespace Worker.Services;

public interface IEmailSender
{
    /// <summary>
    /// Creates EmailMasterData record (Pending). Called by Web app before enqueuing Hangfire job.
    /// </summary>
    Task<long> CreateMasterDataAsync(EmailRequest request);

    /// <summary>
    /// Loads master data, rebuilds body from template, sends via SMTP, logs result.
    /// Retry-able by Hangfire.
    /// </summary>
    Task SendAsync(long masterDataId);
}

