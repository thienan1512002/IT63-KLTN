using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Models.Dtos.Admin;
using Worker.Shared.Data;

namespace Web.Services.Admin;

public class EmailLogService : IEmailLogService
{
    private readonly WorkerDbContext _db;

    public EmailLogService(WorkerDbContext db)
    {
        _db = db;
    }

    public IQueryable<EmailLogRowDto> GetListQuery()
    {
        return _db.EmailMasterDatas
            .Include(e => e.Logs)
            .AsNoTracking()
            .Select(e => new EmailLogRowDto
            {
                Id = e.Id,
                Subject = e.Subject,
                ToAddress = e.ToAddress,
                CcAddresses = e.CcAddresses,
                NotificationType = e.NotificationType != null ? e.NotificationType.ToString() : null,
                WorkflowAction = e.WorkflowAction != null ? e.WorkflowAction.ToString() : null,
                Status = e.Status.ToString(),
                AttemptNumber = e.AttemptNumber,
                CreatedAt = e.CreatedAt,
                SentAt = e.SentAt,
                RetryAt = e.RetryAt,
                LastError = e.Logs
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => l.ErrorMessage)
                    .FirstOrDefault(),
                Attempts = e.Logs
                    .OrderBy(l => l.CreatedAt)
                    .Select(l => new EmailAttemptDto
                    {
                        Id = l.Id,
                        IsSuccess = l.IsSuccess,
                        ErrorMessage = l.ErrorMessage,
                        CreatedAt = l.CreatedAt
                    })
                    .ToList()
            });
    }

    public async Task<IReadOnlyList<EmailAttemptDto>> GetAttemptsAsync(
        long emailId, CancellationToken cancellationToken = default)
    {
        return await _db.EmailLogs
            .AsNoTracking()
            .Where(l => l.EmailMasterDataId == emailId)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new EmailAttemptDto
            {
                Id = l.Id,
                IsSuccess = l.IsSuccess,
                ErrorMessage = l.ErrorMessage,
                CreatedAt = l.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
