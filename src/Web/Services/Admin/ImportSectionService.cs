using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Collections;
using System.Globalization;
using UserModule.Models.ImportModels;
using Web.Data.DbContexts;
using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;
using Web.Models.Enums;

namespace Web.Services.Admin;

public class ImportSectionService : IImportSectionService
{
   
    private const int ColName = 2;   
    private const int ColEngName = 3;
    private const int ColShortName = 4;
    private const int ColBelongSite = 5;
    private const int ColDepartment = 6;
    private const int ColMainApprover = 7;
    private const int ColBackupApprover = 8;
    private const int ColMainLeadApprover = 9;
    private const int ColBackupLeadApprover = 10;
    private const int ColNote = 11;
    private const int ColAction = 12;

    private readonly ApplicationDbContext _db;
    private readonly IBelongSiteService _belongSiteService;
    private readonly IDepartmentService _departmentService;
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _env;

    public ImportSectionService(
        ApplicationDbContext db,
        IBelongSiteService belongSiteService,
        IDepartmentService departmentService,
        IUserService userService,
        IWebHostEnvironment env)
    {
        _db = db;
        _belongSiteService = belongSiteService;
        _departmentService = departmentService;
        _userService = userService;
        _env = env;
    }

    public async Task<byte[]> ExportAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken = default)
    {
       
        var query = _db.Sections
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new SectionExportRow
            {
                Id = s.Id,
                Name = s.Name,
                ExportName = s.Name,
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
                DeputyApprover = s.DeputyApprovers,               
            });

        var loadResult = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
        var rows = ((IEnumerable)loadResult.data).Cast<SectionExportRow>().ToList();

        return AdminExcelTemplateExport.FillTemplate(
            _env,
            "section_template.xlsx",
            ColNote,
            rows,
            (ws, row, _, item) =>
            {
                ws.Cells[row, ColName].Value = item.ExportName;
                ws.Cells[row, ColEngName].Value = item.EngName;
                ws.Cells[row, ColShortName].Value = item.ShortName;
                ws.Cells[row, ColBelongSite].Value = item.BelongSiteName;
                ws.Cells[row, ColDepartment].Value = item.DepartmentName;
                ws.Cells[row, ColMainApprover].Value = item.MainApprover;
                ws.Cells[row, ColBackupApprover].Value = item.BackupApprover;
                ws.Cells[row, ColMainLeadApprover].Value = item.PIC;
                ws.Cells[row, ColBackupLeadApprover].Value = item.DeputyApprover;
                ws.Cells[row, ColNote].Value = item.Note;
            });
    }

    public async Task<ImportResult> ImportAsync(IFormFile excelFile)
    {
        var result = new ImportResult();
        List<SectionImportRow> rows;

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

        // Detect duplicates within file (same Name + Department + Belong Site)
        var duplicates = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name) && !string.IsNullOrWhiteSpace(r.Department) && !string.IsNullOrWhiteSpace(r.BelongSite))
            .GroupBy(r => $"{r.Name}||{r.Department}||{r.BelongSite}", StringComparer.OrdinalIgnoreCase)
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
                    Messages = [$"Duplicate Section '{row.Name}' for Department '{row.Department}' and Belong Site '{row.BelongSite}' in the file."]
                });
                result.Failed++;
            }
            return result;
        }

        result.TotalRows = rows.Count;

        var belongSites = await _belongSiteService.GetLookupsAsync();
        var departments = await _departmentService.GetLookupsAsync();
        var existingUserNames = await _userService.GetExistingUserNamesAsync();

        // Validate all rows first
        foreach (var row in rows)
        {
            var errors = ValidateRow(row, belongSites, departments, existingUserNames);
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

        // Pre-load all existing sections for the referenced departments (single query)
        var deptIds = rows
            .Select(r => departments.FirstOrDefault(d => d.Name.Equals(r.Department, StringComparison.OrdinalIgnoreCase)))
            .Where(d => d is not null)
            .Select(d => d!.Id)
            .Distinct()
            .ToList();

        var existingSections = await _db.Sections
            .Where(s => deptIds.Contains(s.DepartmentId))
            .ToListAsync();

        // Process all rows in memory
        foreach (var row in rows)
        {
            var processError = ProcessRow(row, belongSites, departments, existingSections);
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

    private static List<SectionImportRow> ParseExcel(Stream stream, ImportResult result)
    {
        var rows = new List<SectionImportRow>();
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

            rows.Add(new SectionImportRow
            {
                RowNumber = r,
                Action = action,
                Name = ws.Cells[r, ColName].Value?.ToString()?.Trim() ?? "",
                EngName = ws.Cells[r, ColEngName].Value?.ToString()?.Trim() ?? "",
                Note = ws.Cells[r, ColNote].Value?.ToString()?.Trim(),
                ShortName = ws.Cells[r, ColShortName].Value?.ToString()?.Trim(),
                BelongSite = ws.Cells[r, ColBelongSite].Value?.ToString()?.Trim(),
                Department = ws.Cells[r, ColDepartment].Value?.ToString()?.Trim(),
                MainApprover = ws.Cells[r, ColMainApprover].Value?.ToString()?.Trim(),
                BackupApprover = ws.Cells[r, ColBackupApprover].Value?.ToString()?.Trim(),
                PIC = ws.Cells[r, ColMainLeadApprover].Value?.ToString()?.Trim(),
                DeputyApprovers = ws.Cells[r, ColBackupLeadApprover].Value?.ToString()?.Trim(),
            });
        }

        return rows;
    }

    private List<string> ValidateRow(
        SectionImportRow row,
        IReadOnlyList<BelongSiteLookup> belongSites,
        IReadOnlyList<DepartmentLookup> departments,
        HashSet<string> existingUserNames)
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

        if (string.IsNullOrWhiteSpace(row.Department))
        {
            errors.Add("Department is required.");
        }
        else
        {
            var dept = departments.FirstOrDefault(d =>
                d.Name.Equals(row.Department, StringComparison.OrdinalIgnoreCase));
            if (dept is null)
                errors.Add($"Department '{row.Department}' not found.");
        }

        // TODO:  Main Approvers are required, but Users are not fully imported at this stage,
        //  so we will skip the check for now and do it in the processing stage.


        // Only main approver is required
        // if (string.IsNullOrWhiteSpace(row.MainApprover))
        // {
        //     errors.Add("Main Approver is required.");
        // }
        //else 
        if (! string.IsNullOrWhiteSpace(row.MainApprover) && !existingUserNames.Contains(row.MainApprover))
        {
            errors.Add($"Main Approver '{row.MainApprover}' not found.");
        }

        if (!string.IsNullOrWhiteSpace(row.BackupApprover) && !existingUserNames.Contains(row.BackupApprover))
            errors.Add($"Backup Approver '{row.BackupApprover}' not found.");

        if (!string.IsNullOrWhiteSpace(row.PIC)){
            var pics = row.PIC.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var approver in pics)
            {
                if (existingUserNames.Contains(approver) == false)
                {
                    errors.Add($"PIC '{approver}' not found.");  
                }               
            }           
        }

        if (!string.IsNullOrWhiteSpace(row.DeputyApprovers)){
            var deputyApprovers = row.DeputyApprovers.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var approver in deputyApprovers)
            {
                if (existingUserNames.Contains(approver) == false)
                {
                    errors.Add($"Deputy Approver '{approver}' not found.");
                }
            }
        }

        return errors;
    }

    private string ProcessRow(
        SectionImportRow row,
        IReadOnlyList<BelongSiteLookup> belongSites,
        IReadOnlyList<DepartmentLookup> departments,
        List<Section> existingSections)
    {
        var site = belongSites.First(b =>
            b.Name.Equals(row.BelongSite, StringComparison.OrdinalIgnoreCase));
        var dept = departments.First(d =>
            d.BelongSiteId == site.Id &&
            d.Name.Equals(row.Department, StringComparison.OrdinalIgnoreCase));

        switch (row.Action)
        {
            case ImportAction.Create:
            {
                var existing = existingSections.FirstOrDefault(s =>
                    s.DepartmentId == dept.Id &&
                    s.Name.Equals(row.Name, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    if (existing.IsActive)
                        return $"Section '{row.Name}' already exists for Department '{row.Department}'.";

                    // Reactivate soft-deleted record to avoid unique constraint violation
                    existing.Name = row.Name;
                    existing.EngName = row.EngName;
                    existing.Note = row.Note;
                    existing.ShortName = row.ShortName;
                    existing.SiteId = site.Id;
                    existing.MainApprover = row.MainApprover;
                    existing.BackupApprover = row.BackupApprover;
                    existing.PIC = row.PIC;
                    existing.DeputyApprovers = row.DeputyApprovers;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.Now;
                    return null;
                }

                var newSection = new Section
                {
                    Name = row.Name,
                    EngName = row.EngName,
                    Note = row.Note,
                    ShortName = row.ShortName,
                    DepartmentId = dept.Id,
                    SiteId = site.Id,
                    MainApprover = row.MainApprover,
                    BackupApprover = row.BackupApprover,
                    PIC = row.PIC,
                    DeputyApprovers = row.DeputyApprovers,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _db.Sections.Add(newSection);
                existingSections.Add(newSection);
                return null;
            }
            case ImportAction.Update:
            {
                var entity = existingSections.FirstOrDefault(s =>
                    //s.IsActive &&
                    s.DepartmentId == dept.Id &&
                    s.Name.Equals(row.Name, StringComparison.OrdinalIgnoreCase));
                if (entity is null)
                    return $"Section '{row.Name}' not found for Department '{row.Department}'.";

                entity.Note = row.Note;
                entity.EngName = row.EngName;
                entity.SiteId = site.Id;
                entity.MainApprover = row.MainApprover;
                entity.BackupApprover = row.BackupApprover;
                entity.PIC = row.PIC;
                entity.DeputyApprovers = row.DeputyApprovers;
                entity.IsActive = true;
                entity.UpdatedAt = DateTime.Now;
                return null;
            }
            case ImportAction.Delete:
            {
                var entity = existingSections.FirstOrDefault(s =>
                    s.IsActive &&
                    s.DepartmentId == dept.Id &&
                    s.Name.Equals(row.Name, StringComparison.OrdinalIgnoreCase));
                if (entity is null)
                    return $"Section '{row.Name}' not found for Department '{row.Department}'.";

                entity.IsActive = false;
                entity.UpdatedAt = DateTime.Now;
                return null;
            }
            default:
                return "Unknown action.";
        }
    }

    private sealed class SectionExportRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ExportName { get; set; }
        public string EngName { get; set; }
        public string Note { get; set; }
        public string ShortName { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int BelongSiteId { get; set; }
        public string BelongSiteName { get; set; }
        public string MainApprover { get; set; }
        public string BackupApprover { get; set; }
        public string PIC { get; set; }
        public string DeputyApprover { get; set; }
       
    }
}
