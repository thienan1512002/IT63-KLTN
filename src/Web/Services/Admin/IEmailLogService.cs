using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public interface IEmailLogService
{
    /// <summary>Returns a composable query for the DataGrid (server-side paging/filtering/sorting).</summary>
    IQueryable<EmailLogRowDto> GetListQuery();

    /// <summary>Returns all send-attempt log entries for one email master record.</summary>
    Task<IReadOnlyList<EmailAttemptDto>> GetAttemptsAsync(long emailId, CancellationToken cancellationToken = default);
}
