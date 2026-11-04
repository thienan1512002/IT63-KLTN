using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Collections;
using UserModule.Models.ImportModels;
using Web.Data.DbContexts;
using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;
using Web.Models.Enums;

namespace Web.Services.Admin;

public class ImportDepartmentService : IImportDepartmentService
{    
    private const int ColName = 2;
    private const int ColShortName = 3;    
    private const int ColBelongSite = 4;
    private const int ColMainApprover = 5;
    private const int ColBackupApprover = 6;
    private const int ColDescription = 7;
    private const int ColAction = 8;

    private readonly ApplicationDbContext _db;
    private readonly IBelongSiteService _belongSiteService;
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _env;

    public ImportDepartmentService(
        ApplicationDbContext db,
        IBelongSiteService belongSiteService,
        IUserService userService,
        IWebHostEnvironment env)
    {
        _db = db;
        _belongSiteService = belongSiteService;
        _userService = userService;
        _env = env;
    }

    public async Task<byte[]> ExportAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken = default)
    {
        var query = _db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new DepartmentExportRow
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
                UpdatedAt = d.UpdatedAt
            });

        var loadResult = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
        var rows = ((IEnumerable)loadResult.data).Cast<DepartmentExportRow>().ToList();

        return AdminExcelTemplateExport.FillTemplate(
            _env,
            "department_template.xlsx",
            ColDescription,
            rows,
            (ws, row, _, item) =>
            {
                ws.Cells[row, ColName].Value = item.Name;
                ws.Cells[row, ColShortName].Value = item.ShortName;
                ws.Cells[row, ColBelongSite].Value = item.BelongSiteName;
                ws.Cells[row, ColMainApprover].Value = item.MainApprover;
                ws.Cells[row, ColBackupApprover].Value = item.BackupApprover;
                ws.Cells[row, ColDescription].Value = item.Description;
            });
    }

    public async Task<ImportResult> ImportAsync(IFormFile excelFile)
    {
        var result = new ImportResult();
        List<DepartmentImportRow> rows;

        try
        {
            using var stream = excelFile.OpenReadStream();
            rows = ParseExcel(stream, result);
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportRowError
            {
                Row = 0,
                Action = "N/A",
                Messages = [$"Failed to read Excel file: {ex.Message}"]
            });
            return result;
        }

        // Detect duplicates within file (same Name + BelongSite)
        var duplicates = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name) && !string.IsNullOrWhiteSpace(r.BelongSite))
            .GroupBy(r => $"{r.Name}||{r.BelongSite}", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        if (duplicates.Count > 0)
        {
            foreach (var row in duplicates)
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = row.RowNumber,
                    Issuer = row.Name,
                    Action = row.Action.ToString(),
                    Messages = [$"Duplicate Department '{row.Name}' for BelongSite '{row.BelongSite}' in the file."]
                });
                result.Failed++;
            }
            return result;
        }

        result.TotalRows = rows.Count;

        var belongSites = await _belongSiteService.GetLookupsAsync();
        var existingUserNames = await _userService.GetExistingUserNamesAsync();

        // Validate all rows first
        foreach (var row in rows)
        {
            var errors = ValidateRow(row, belongSites, existingUserNames);
            if (errors.Count > 0)
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = row.RowNumber,
                    Issuer = row.Name,
                    Action = row.Action.ToString(),
                    Messages = errors
                });
                result.Failed++;
            }
        }

        if (result.Failed > 0)
            return result;

        // Pre-load all existing departments for the referenced sites (single query)
        var siteIds = rows
            .Select(r => belongSites.FirstOrDefault(b => b.Name.Equals(r.BelongSite, StringComparison.OrdinalIgnoreCase)))
            .Where(b => b is not null)
            .Select(b => b!.Id)
            .Distinct()
            .ToList();
        
        var existingDepartments = await _db.Departments
            .Where(d => siteIds.Contains(d.SiteId))
            .ToListAsync();

        // Process all rows in memory
        foreach (var row in rows)
        {
            var processError = ProcessRow(row, belongSites, existingDepartments);
            if (processError is not null)
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = row.RowNumber,
                    Issuer = row.Name,
                    Action = row.Action.ToString(),
                    Messages = [processError]
                });
                result.Failed++;
            }
            else
            {
                result.Succeeded++;
            }
        }

        if (result.Failed > 0)
        {
            result.Succeeded = 0;
            return result;
        }

        // Save all changes in a single transaction
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Succeeded = 0;
            result.Errors.Add(new ImportRowError
            {
                Row = 0,
                Action = "N/A",
                Messages = [$"Unexpected error during import: {ex.Message}"]
            });
        }

        return result;
    }

    private static List<DepartmentImportRow> ParseExcel(Stream stream, ImportResult result)
    {
        var rows = new List<DepartmentImportRow>();
        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets[0];

        if (ws.Dimension is null)
            return rows;

        int endRow = ws.Dimension.End.Row;
        for (int r = 5; r <= endRow; r++)
        {
            var actionRaw = ws.Cells[r, ColAction].Value?.ToString()?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(actionRaw))
                continue;

            if (!ImportActionHelper.TryParse(actionRaw, out var action))
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = r,
                    Issuer = ws.Cells[r, ColName].Value?.ToString() ?? "",
                    Action = actionRaw,
                    Messages = [$"Invalid Action '{actionRaw}'. Must be C, U, or D."]
                });
                result.Failed++;
                continue;
            }

            rows.Add(new DepartmentImportRow
            {
                RowNumber = r,
                Action = action,
                Name = ws.Cells[r, ColName].Value?.ToString()?.Trim() ?? "",
                ShortName = ws.Cells[r, ColShortName].Value?.ToString()?.Trim(),
                Description = ws.Cells[r, ColDescription].Value?.ToString()?.Trim(),
                BelongSite = ws.Cells[r, ColBelongSite].Value?.ToString()?.Trim(),
                MainApprover = ws.Cells[r, ColMainApprover].Value?.ToString()?.Trim(),
                BackupApprover = ws.Cells[r, ColBackupApprover].Value?.ToString()?.Trim(),
 
            });
        }

        return rows;
    }

    private List<string> ValidateRow(DepartmentImportRow row, IReadOnlyList<BelongSiteLookup> belongSites, HashSet<string> existingUserNames)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            errors.Add("Name is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(row.BelongSite))
        {
            errors.Add("BelongSite is required.");
        }
        else
        {
            var site = belongSites.FirstOrDefault(b =>
                b.Name.Equals(row.BelongSite, StringComparison.OrdinalIgnoreCase));
            if (site is null)
                errors.Add($"BelongSite '{row.BelongSite}' not found.");
        }

        // TODO:  Main Approvers are required, but Users are not fully imported at this stage,
        //  so we will skip the check for now and do it in the processing stage.
       
        // Check if approver usernames exist in the system, only main approver is required
        // if (string.IsNullOrWhiteSpace(row.MainApprover))
        // {
        //     errors.Add("Main Approver is required.");
        // }
        // else
        if (!string.IsNullOrWhiteSpace(row.MainApprover) && !existingUserNames.Contains(row.MainApprover))
        {
            errors.Add($"Main Approver '{row.MainApprover}' not found.");
        }

        if (!string.IsNullOrWhiteSpace(row.BackupApprover) && !existingUserNames.Contains(row.BackupApprover))
            errors.Add($"Backup Approver '{row.BackupApprover}' not found.");

        return errors;
    }

    private string ProcessRow(
        DepartmentImportRow row,
        IReadOnlyList<BelongSiteLookup> belongSites,
        List<Department> existingDepartments)
    {
        var site = belongSites.First(b =>
            b.Name.Equals(row.BelongSite, StringComparison.OrdinalIgnoreCase));

        switch (row.Action)
        {
            case ImportAction.Create:
            {
                var existing = existingDepartments.FirstOrDefault(d =>
                    d.SiteId == site.Id &&
                    d.Name.Equals(row.Name, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    if (existing.IsActive)
                        return $"Department '{row.Name}' already exists for this BelongSite.";

                    // Reactivate soft-deleted record to avoid unique constraint violation
                    existing.ShortName = row.ShortName;
                    existing.Description = row.Description;
                    existing.MainApprover = row.MainApprover;
                    existing.BackupApprover = row.BackupApprover;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.Now;
                    return null;
                }

                var newDept = new Department
                {
                    Name = row.Name,
                    ShortName = row.ShortName,
                    Description = row.Description,
                    SiteId = site.Id,
                    MainApprover = row.MainApprover,
                    BackupApprover = row.BackupApprover,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _db.Departments.Add(newDept);
                existingDepartments.Add(newDept);
                return null;
            }
            case ImportAction.Update:
            {
                var entity = existingDepartments.FirstOrDefault(d => 
                    //d.IsActive = true &&                 
                    d.SiteId == site.Id &&
                    d.Name.Equals(row.Name, StringComparison.OrdinalIgnoreCase));
                if (entity is null)
                    return $"Department '{row.Name}' not found for BelongSite '{row.BelongSite}'.";

                entity.Description = row.Description;
                entity.MainApprover = row.MainApprover;
                entity.BackupApprover = row.BackupApprover;
                entity.UpdatedAt = DateTime.Now;
                entity.IsActive = true;
                return null;
            }
            case ImportAction.Delete:
            {
                var entity = existingDepartments.FirstOrDefault(d =>
                    d.IsActive &&   
                    d.SiteId == site.Id &&
                    d.Name.Equals(row.Name, StringComparison.OrdinalIgnoreCase));
                if (entity is null)
                    return $"Department '{row.Name}' not found for BelongSite '{row.BelongSite}'.";

                entity.IsActive = false;
                entity.UpdatedAt = DateTime.Now;
                return null;
            }
            default:
                return "Unknown action.";
        }
    }

    private sealed class DepartmentExportRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public int BelongSiteId { get; set; }
        public string BelongSiteName { get; set; }
        public string MainApprover { get; set; }
        public string BackupApprover { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
