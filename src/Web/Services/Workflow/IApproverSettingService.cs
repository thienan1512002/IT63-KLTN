using Web.Models.Dtos.Workflow.ApproverSetting;

namespace Web.Services.Workflow;

public interface IApproverSettingService
{
    Task<IReadOnlyList<ApproverSettingListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ApproverSettingUpsertDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, ApproverSettingUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
