using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Web.Data.DbContexts;
using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;
using Web.Models.Dtos.Workflow;

namespace Web.Services.Admin;

public class SectionService : ISectionService
{
    private readonly ApplicationDbContext _db;

    public SectionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public IQueryable<SectionListItemDto> GetListQuery()
    {
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        return _db.Sections
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Department)
            .Include(s => s.BelongSite)
            .Select(s => new SectionListItemDto
            {
                Id = s.Id,
                Name = isVi ? s.Name : s.EngName,
                EngName = s.EngName,
                Note = s.Note,
                ShortName = s.ShortName,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department.Name,
                BelongSiteId = s.SiteId,
                BelongSiteName = s.BelongSite.Name,
                MainApprover = s.MainApprover,
                BackupApprover = s.BackupApprover,
                PIC = s.PIC,
                DeputyApprovers = s.DeputyApprovers,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CreatedBy = s.CreatedBy,
                UpdatedBy = s.UpdatedBy
            });
    }

    public async Task<IReadOnlyList<SectionLookup>> GetLookupsAsync()
    {
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        return await _db.Sections
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new SectionLookup(s.Id, isVi ? s.Name : s.EngName, s.EngName, s.DepartmentId))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SectionLookup>> GetLookupsBySiteIdAsync(int siteId)
    {
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        return await _db.Sections
                .AsNoTracking()
                .Where(s => s.IsActive && s.SiteId == siteId)
                .Select(s => new SectionLookup(s.Id, isVi ? s.Name : s.EngName, s.EngName, s.DepartmentId))
                .ToListAsync();
    }

    public async Task<IReadOnlyList<SectionLookupMasterData>> GetSectionLookupMasterData(int siteId)
    {
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var masterDataRaw = await _db.Sections
                .AsNoTracking()
                .Where(s => s.IsActive && s.SiteId == siteId)
                .Select(s => new
                {
                    Id = s.Id,
                    Name = s.Name,
                    EngName = s.EngName,
                    MainApprover = s.MainApprover ?? string.Empty,
                    BackupApprover = s.BackupApprover ?? string.Empty,
                    MainLeadApprover = s.PIC ?? string.Empty,
                })
                .ToListAsync();

        var allApprovers = masterDataRaw
            .SelectMany(s => new[] { s.MainApprover, s.BackupApprover }
                .Concat(s.MainLeadApprover.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .Where(a => !string.IsNullOrEmpty(a))
            .Distinct()
            .ToList();

        var approverInfoDict = await _db.AppUsers
            .Include(u => u.Section)
            .AsNoTracking()
            .Where(u => allApprovers.Contains(u.UserName))
            .ToDictionaryAsync(u => u.UserName, u => new ApproverInfoDto()
            {
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                SectionName = u.Section != null ? (isVi ? u.Section.Name : u.Section.EngName) : string.Empty,
                Position = u.Position,
                PhoneNumber = u.PhoneNumber,
                IsOffline = u.IsOnline == false,
            });


        var masterDataList = new List<SectionLookupMasterData>();

        foreach (var item in masterDataRaw)
        {

            var masterData = new SectionLookupMasterData
            {
                Id = item.Id,
                Name = isVi ? item.Name : item.EngName,
                EngName = item.EngName,
                ApproverInfo = new List<ApproverInfoDto>(),
                PICApproverInfo = new List<ApproverInfoDto>()
            };

            var mainApproverInfo = approverInfoDict.ContainsKey(item.MainApprover) ? approverInfoDict[item.MainApprover] : null;
            var backupApproverInfo = !String.IsNullOrEmpty(item.BackupApprover) && approverInfoDict.ContainsKey(item.BackupApprover) ? approverInfoDict[item.BackupApprover] : null;

            if (mainApproverInfo != null)
            {
                masterData.ApproverInfo.Add(mainApproverInfo);
            }
            if (backupApproverInfo != null)
            {
                masterData.ApproverInfo.Add(backupApproverInfo);
            }

            if (!String.IsNullOrWhiteSpace(item.MainLeadApprover))
            {
                var pics = item.MainLeadApprover.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var pic in pics)
                {
                    if (approverInfoDict.ContainsKey(pic))
                    {
                        masterData.PICApproverInfo.Add(approverInfoDict[pic]);
                    }
                }
            }

            masterDataList.Add(masterData);
        }

        return masterDataList;
    }

    public async Task<Section> GetByIdAsync(int id)
    {
        return await _db.Sections.FindAsync(id);
    }

    public async Task<Section> CreateAsync(CreateSectionRequestDto request)
    {
        // Create a helper method to validate input data is valid or not, and throw exception if not valid. This will make the main logic cleaner.
        await ValidateSectionRequest(request);
        var entity = new Section
        {
            Name = request.Name?.Trim(),
            EngName = request.EngName?.Trim(),
            Note = request.Note?.Trim(),
            ShortName = request.ShortName?.Trim(),
            DepartmentId = request.DepartmentId,
            SiteId = request.BelongSiteId,
            MainApprover = request.MainApprover?.Trim(),
            BackupApprover = request.BackupApprover?.Trim(),
            PIC = request.PIC?.Trim(),
            DeputyApprovers = request.DeputyApprovers?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _db.Sections.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(int id, UpdateSectionRequestDto request)
    {
        var entity = await _db.Sections.FindAsync(id)
            ?? throw new KeyNotFoundException($"Section with Id {id} not found.");

        await ValidateSectionRequest(request);

        entity.Name = request.Name?.Trim();
        entity.EngName = request.EngName?.Trim();
        entity.Note = request.Note?.Trim();
        entity.ShortName = request.ShortName?.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.SiteId = request.BelongSiteId;
        entity.MainApprover = request.MainApprover?.Trim();
        entity.BackupApprover = request.BackupApprover?.Trim();
        entity.PIC = request.PIC?.Trim();
        entity.DeputyApprovers = request.DeputyApprovers?.Trim();
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task SetActiveStatusAsync(int id, bool isActive)
    {
        var entity = await _db.Sections.FindAsync(id)
            ?? throw new KeyNotFoundException($"Section with Id {id} not found.");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    private async Task ValidateSectionRequest(CreateSectionRequestDto request)
    {
        // Validate if belong site exists
        var siteExists = await _db.BelongSites.AnyAsync(s => s.Id == request.BelongSiteId);
        if (!siteExists) throw new InvalidDataException($"BelongSite with Id {request.BelongSiteId} not found.");

        // Validate if department exists
        var deptExists = await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId);
        if (!deptExists) throw new InvalidDataException($"Department with Id {request.DepartmentId} not found.");

        // Validate all approvers exist in the system
        var approversToCheck = new HashSet<string>();
        if (!string.IsNullOrEmpty(request.MainApprover))
            approversToCheck.Add(request.MainApprover.Trim());
        if (!string.IsNullOrEmpty(request.BackupApprover))
            approversToCheck.Add(request.BackupApprover.Trim());
        if (!string.IsNullOrEmpty(request.PIC))
        {
            var mainLeadApprovers = request.PIC.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var approver in mainLeadApprovers)
                approversToCheck.Add(approver);
        }
        if (!string.IsNullOrEmpty(request.DeputyApprovers))
        {
            var deputyApprovers = request.DeputyApprovers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var approver in deputyApprovers)
                approversToCheck.Add(approver);
        }

        var existingApprovers = await _db.AppUsers
            .AsNoTracking()
            .Where(u => approversToCheck.Contains(u.UserName))
            .Select(u => u.UserName)
            .ToListAsync();

        var missingApprovers = approversToCheck.Except(existingApprovers).ToList();
        if (missingApprovers.Any())
        {
            throw new InvalidDataException($"The following approvers were not found: {string.Join(", ", missingApprovers)}");
        }
    }
}