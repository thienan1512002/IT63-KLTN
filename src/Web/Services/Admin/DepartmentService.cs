using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _db;

    public DepartmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public IQueryable<DepartmentListItemDto> GetListQuery()
    {
        return _db.Departments
            .AsNoTracking()
            .Include(d => d.BelongSite)
            .Select(d => new DepartmentListItemDto
            {
                Id = d.Id,
                Name = d.Name,
                ShortName = d.ShortName,
                Description = d.Description,
                BelongSiteId = d.SiteId,
                BelongSiteName = d.BelongSite.Name,
                MainApprover = d.MainApprover,
                BackupApprover = d.BackupApprover,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                CreatedBy = d.CreatedBy,
                UpdatedBy = d.UpdatedBy
            });
    }

    public async Task<IReadOnlyList<DepartmentLookup>> GetLookupsAsync()
        => await _db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new DepartmentLookup(d.Id, d.Name, d.SiteId))
            .ToListAsync();

    public async Task<Department> GetByIdAsync(int id)
    {
        return await _db.Departments.FindAsync(id);
    }

    public async Task<Department> CreateAsync(CreateDepartmentRequestDto request)
    {
        await ValidateDepartmentRequest(request);

        var entity = new Department
        {
            Name = request.Name.Trim(),
            ShortName = request.ShortName?.Trim(),
            Description = request.Description?.Trim(),
            SiteId = request.BelongSiteId,
            MainApprover = request.MainApprover?.Trim(),
            BackupApprover = request.BackupApprover?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _db.Departments.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(int id, UpdateDepartmentRequestDto request)
    {
        var entity = await _db.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Department with Id {id} not found.");

        await ValidateDepartmentRequest(request);

        entity.Name = request.Name?.Trim();
        entity.ShortName = request.ShortName?.Trim();
        entity.Description = request.Description?.Trim();
        entity.SiteId = request.BelongSiteId;
        entity.MainApprover = request.MainApprover?.Trim();
        entity.BackupApprover = request.BackupApprover?.Trim();
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task SetActiveStatusAsync(int id, bool isActive)
    {
        var entity = await _db.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Department with Id {id} not found.");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task<HashSet<string>> GetExistingNamesAsync(int belongSiteId)
    {
        var names = await _db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive && d.SiteId == belongSiteId)
            .Select(d => d.Name)
            .ToListAsync();

        return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task ValidateDepartmentRequest(CreateDepartmentRequestDto request)
    {
        // Validate if belong site exists
        var siteExists = await _db.BelongSites.AnyAsync(s => s.Id == request.BelongSiteId);
        if (!siteExists) throw new InvalidDataException($"BelongSite with Id {request.BelongSiteId} not found.");

        // Validate all approvers exist in the system
        var approversToCheck = new HashSet<string>();
        if (!string.IsNullOrWhiteSpace(request.MainApprover)) approversToCheck.Add(request.MainApprover.Trim());
        if (!string.IsNullOrWhiteSpace(request.BackupApprover)) approversToCheck.Add(request.BackupApprover.Trim());

        if (approversToCheck.Count == 0) return; // No approvers to check

        var existingApprovers = await _db.AppUsers
            .AsNoTracking()
            .Where(u => approversToCheck.Contains(u.UserName))
            .Select(u => u.UserName)
            .ToListAsync();

        var missingApprovers = approversToCheck.Except(existingApprovers).ToList();
        if (missingApprovers.Any())
        {
            throw new InvalidDataException($"The following approvers do not exist: {string.Join(", ", missingApprovers)}");
        }
    }
}
