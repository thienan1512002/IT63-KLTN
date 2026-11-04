using Web.Models.Dtos.Workflow;

namespace Web.Services.Workflow;

public interface IRequestService
{
    IQueryable<RequestListItemDto> BuildMyRequestsQuery(string userId);
    IQueryable<RequestListItemDto> BuildPendingApprovalsQuery(string userId);
    Task<RequestDetailsDto> GetDetailsAsync(int id, string userId, bool isAdmin, CancellationToken ct = default);
    Task<int> CreateDraftAsync(RequestEditDto input, string userId, IEnumerable<IFormFile> files, CancellationToken ct = default);
    Task UpdateDraftAsync(int id, RequestEditDto input, string userId, IEnumerable<IFormFile> files, CancellationToken ct = default);
    Task SubmitAsync(int id, string userId, CancellationToken ct = default);
    Task ApproveAsync(int id, string userId, string? comment, CancellationToken ct = default);
    Task ReturnAsync(int id, string userId, string comment, CancellationToken ct = default);
    Task RejectAsync(int id, string userId, string comment, CancellationToken ct = default);
    Task CancelAsync(int id, string userId, string? comment, CancellationToken ct = default);
}
