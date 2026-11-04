using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Models.Dtos.Workflow.ApproverSetting;

namespace Web.Services.Workflow;

public class ApproverSettingService : IApproverSettingService
{
    private readonly ApplicationDbContext _db;

    public ApproverSettingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ApproverSettingListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.FormApprovers
            .AsNoTracking()
            .Where(x => x.IsRemoved == false)
            .OrderBy(x => x.FormCode)
            .ThenBy(x => x.Title)
            .Select(x => new ApproverSettingListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                DisplayTitle = x.DisplayTitle,
                FormCode = x.FormCode,
                UserNames = x.UserNames,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(ApproverSettingUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new FormApproverSetting
        {
            Title = dto.Title?.Trim() ?? string.Empty,
            DisplayTitle = dto.DisplayTitle?.Trim() ?? string.Empty,
            FormCode = dto.FormCode?.Trim() ?? string.Empty,
            UserNames = dto.UserNames?.Trim() ?? string.Empty,
            IsRemoved = false,
        };

        _db.FormApprovers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateAsync(int id, ApproverSettingUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FormApprovers
            .Where(x => x.Id == id && x.IsRemoved == false)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Approver setting {id} not found.");

        entity.Title = dto.Title?.Trim() ?? string.Empty;
        entity.DisplayTitle = dto.DisplayTitle?.Trim() ?? string.Empty;
        entity.FormCode = dto.FormCode?.Trim() ?? string.Empty;
        entity.UserNames = dto.UserNames?.Trim() ?? string.Empty;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FormApprovers
            .Where(x => x.Id == id && x.IsRemoved == false)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null) return false;

        entity.IsRemoved = true;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
