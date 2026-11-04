
using Microsoft.AspNetCore.Identity;
using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Collections;
using System.Globalization;
using UserModule.Models.ImportModels;
using Web.Data.DbContexts;
using Web.Data.Entities.Admin;
using Web.Data.Entities.Identity;
using Web.Data.Seeding;
using Web.Models.Dtos.Identity;
using Web.Services.Admin;
using Identity.Services;
using Web.Models.Enums;


namespace Web.Services.Admin;

public class ImportUserService : IImportUserService
{
    // Expected column order in the Excel template   
    private const int ColUserName = 2;
    private const int ColEmpId = 3;
    private const int ColFullName = 4;
    private const int ColDateOfBirth = 5;
    private const int ColPhoneNumber = 6;
    private const int ColEmail = 7;
    private const int ColPosition = 8;
    private const int ColSection = 9;
    private const int ColDepartment = 10;
    private const int ColBelongSite = 11;

    private const int ColDefaultPassword = 12;
    private const int ColAction = 13;

    private readonly ApplicationDbContext _db;
    private readonly IUserService _userService;
    private readonly IBelongSiteService _belongSiteService;
    private readonly IDepartmentService _departmentService;
    private readonly ISectionService _sectionService;
    private readonly IAccountService _accountService;
    private readonly IWebHostEnvironment _env;

    public ImportUserService(
        ApplicationDbContext db,
        IUserService userService,
        IBelongSiteService belongSiteService,
        IDepartmentService departmentService,
        ISectionService sectionService,
        IAccountService accountService,
        IWebHostEnvironment env)
    {
        _db = db;
        _userService = userService;
        _belongSiteService = belongSiteService;
        _departmentService = departmentService;
        _sectionService = sectionService;
        _accountService = accountService;
        _env = env;
    }

    public async Task<byte[]> ExportAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken = default)
    {
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var query = _db.AppUsers
            .AsNoTracking()
            .Where(user => user.Id != IdentityModelSeed.AdminUserId)
            .Select(user => new UserExportRow
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                EmpId = user.EmpId,
                SectionId = user.SectionId,
                SectionName = user.Section != null ? (isVi ? user.Section.Name : user.Section.EngName) : string.Empty,
                DepartmentId = user.Section.DepartmentId,
                DepartmentName = user.Section.Department.Name,
                BelongSiteId = user.SiteId,
                BelongSiteName = user.BelongSite.Name,
                Position = user.Position,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
            });

        var loadResult = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
        var rows = ((IEnumerable)loadResult.data).Cast<UserExportRow>().ToList();

        return AdminExcelTemplateExport.FillTemplate(
            _env,
            "user_template.xlsx",
            ColBelongSite,
            rows,
            (ws, row, _, item) =>
            {
                ws.Cells[row, ColUserName].Value = item.UserName;
                ws.Cells[row, ColEmpId].Value = item.EmpId;
                ws.Cells[row, ColFullName].Value = item.FullName;
                ws.Cells[row, ColDateOfBirth].Value = item.DateOfBirth;
                ws.Cells[row, ColDateOfBirth].Style.Numberformat.Format = "yyyy/mm/dd";
                ws.Cells[row, ColPhoneNumber].Value = item.PhoneNumber;
                ws.Cells[row, ColEmail].Value = item.Email;
                ws.Cells[row, ColPosition].Value = item.Position;
                ws.Cells[row, ColSection].Value = item.SectionName;
                ws.Cells[row, ColDepartment].Value = item.DepartmentName;
                ws.Cells[row, ColBelongSite].Value = item.BelongSiteName;
            });
    }

    public async Task<ImportResult> ImportUserAsync(IFormFile excelFile)
    {
        var result = new ImportResult();
        List<UserImportRow> rows;

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

        // Check 

        // Detect duplicate UserNames within the file
        var duplicateUserNames = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.UserName))
            .GroupBy(r => r.UserName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (duplicateUserNames.Count > 0)
        {
            foreach (var row in rows.Where(r => duplicateUserNames.Contains(r.UserName)))
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = row.RowNumber,
                    Issuer = row.UserName,
                    Action = row.Action.ToString(),
                    Messages = [$"Duplicate UserName '{row.UserName}' in the file. Each UserName must appear only once."]
                });
                result.Failed++;
            }
            return result; // Stop processing if there are duplicates in the file
        }

        // Detect duplicate EmpIds within the file
        var duplicateEmpIds = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.EmpId))
            .GroupBy(r => r.EmpId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (duplicateEmpIds.Count > 0)
        {
            foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r.EmpId) && duplicateEmpIds.Contains(r.EmpId!.Trim())))
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = row.RowNumber,
                    Issuer = row.UserName,
                    Action = row.Action.ToString(),
                    Messages = [$"Duplicate EmpId '{row.EmpId}' in the file. Each EmpId must appear only once."]
                });
                result.Failed++;
            }
            return result;
        }

        result.TotalRows = rows.Count;

        // Pre-load lookup tables once
        var departments = await _departmentService.GetLookupsAsync();
        var sections = await _sectionService.GetLookupsAsync();
        var belongSites = await _belongSiteService.GetLookupsAsync();

        // Get existing usernames from the database to check for existence
        var existUserNames = await _userService.GetExistingUserNamesAsync();
        var existingEmpIdOwners = (await _db.AppUsers
                .AsNoTracking()
                .Where(user => user.EmpId != null && user.EmpId != string.Empty)
                .Select(user => new { user.EmpId, user.UserName })
                .ToListAsync())
            .Where(user => !string.IsNullOrWhiteSpace(user.EmpId))
            .GroupBy(user => user.EmpId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().UserName ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        // Validate all rows first before touching the database
        foreach (var row in rows)
        {
            var errors = await ValidateRowAsync(row, departments, sections, belongSites, existUserNames, existingEmpIdOwners);

            if (errors.Count > 0)
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = row.RowNumber,
                    Issuer = row.UserName,
                    Action = row.Action.ToString(),
                    Messages = errors
                });
                result.Failed++;
            }
        }

        // Stop if any validation errors — do not touch the database
        if (result.Failed > 0)
            return result;

        // All rows are valid — process inside a single transaction (all or nothing)
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var row in rows)
            {
                var processError = await ProcessRowAsync(row, departments, sections, belongSites);
                if (processError is not null)
                {
                    result.Errors.Add(new ImportRowError
                    {
                        Row = row.RowNumber,
                        Issuer = row.UserName,
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
                await transaction.RollbackAsync();
                result.Succeeded = 0;
            }
            else
            {
                await transaction.CommitAsync();
            }
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

    // ── Parse ────────────────────────────────────────────────────────────────

    private static List<UserImportRow> ParseExcel(Stream stream, ImportResult result)
    {
        var rows = new List<UserImportRow>();

        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets[0]; // first sheet, 1-indexed

        if (ws.Dimension is null)
            return rows;
        int endRow = ws.Dimension.End.Row;
        for (int r = 5; r <= endRow; r++) // row 1 = header
        {
            var actionRaw = ws.Cells[r, ColAction].Value?.ToString()?.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(actionRaw))
                continue; // skip completely empty rows

            if (!ImportActionHelper.TryParse(actionRaw, out var action))
            {
                result.Errors.Add(new ImportRowError
                {
                    Row = r,
                    Issuer = ws.Cells[r, ColUserName].Value?.ToString() ?? "",
                    Action = actionRaw,
                    Messages = [$"Invalid Action '{actionRaw}'. Must be C, U, or D."]
                });
                result.Failed++;
                continue;
            }

            rows.Add(new UserImportRow
            {
                RowNumber = r,
                Action = action,
                UserName = ws.Cells[r, ColUserName].Value?.ToString()?.Trim() ?? "",
                EmpId = ws.Cells[r, ColEmpId].Value?.ToString()?.Trim(),
                FullName = ws.Cells[r, ColFullName].Value?.ToString()?.Trim(),
                Email = ws.Cells[r, ColEmail].Value?.ToString()?.Trim(),
                DefaultPassword = ws.Cells[r, ColDefaultPassword].Value?.ToString()?.Trim(),
                BelongSite = ws.Cells[r, ColBelongSite].Value?.ToString()?.Trim(),
                Department = ws.Cells[r, ColDepartment].Value?.ToString()?.Trim(),
                Section = ws.Cells[r, ColSection].Value?.ToString()?.Trim(),
                Position = ws.Cells[r, ColPosition].Value?.ToString()?.Trim(),
                DateOfBirth = GetCellValueAsDobString(ws.Cells[r, ColDateOfBirth]),
                PhoneNumber = ws.Cells[r, ColPhoneNumber].Value?.ToString()?.Trim(),
            });
        }

        return rows;
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    private async Task<List<string>> ValidateRowAsync(
        UserImportRow row,
        IReadOnlyList<DepartmentLookup> departments,
        IReadOnlyList<SectionLookup> sections,
        IReadOnlyList<BelongSiteLookup> belongSites,
        HashSet<string> existUserNames,
        IReadOnlyDictionary<string, string> existingEmpIdOwners
        )
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.UserName))
        {
            errors.Add("UserName is required.");
            return errors; // can't validate further without username
        }

        switch (row.Action)
        {
            case ImportAction.Create:
                if (existUserNames.Contains(row.UserName))
                    errors.Add($"UserName '{row.UserName}' already exists.");
                ValidateEmpIdUnique(row, existingEmpIdOwners, errors);
                ValidateCreateUpdate(row, departments, sections, belongSites, errors);
                break;

            case ImportAction.Update:
                if (!existUserNames.Contains(row.UserName))
                    errors.Add($"UserName '{row.UserName}' not found.");
                ValidateEmpIdUnique(row, existingEmpIdOwners, errors);
                ValidateCreateUpdate(row, departments, sections, belongSites, errors);
                break;

            case ImportAction.Delete:
                if (!existUserNames.Contains(row.UserName))
                    errors.Add($"UserName '{row.UserName}' not found.");
                break;
        }

        // TODO: Validate Position value if needed      

        return errors;
    }

    private static void ValidateEmpIdUnique(
        UserImportRow row,
        IReadOnlyDictionary<string, string> existingEmpIdOwners,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(row.EmpId))
            return;

        var empId = row.EmpId.Trim();
        if (!existingEmpIdOwners.TryGetValue(empId, out var ownerUserName))
            return;

        if (row.Action == ImportAction.Create ||
            !string.Equals(ownerUserName, row.UserName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"EmpId '{empId}' already exists.");
        }
    }

    private static void ValidateCreateUpdate(
        UserImportRow row,
        IReadOnlyList<DepartmentLookup> departments,
        IReadOnlyList<SectionLookup> sections,
        IReadOnlyList<BelongSiteLookup> belongSites,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(row.EmpId))
            errors.Add("EmpId is required.");

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

        if (string.IsNullOrWhiteSpace(row.FullName))
            errors.Add("FullName is required.");

        if (!string.IsNullOrWhiteSpace(row.Email) && !IsValidEmail(row.Email))
            errors.Add($"Email '{row.Email}' is not a valid email address.");

        if (string.IsNullOrWhiteSpace(row.DefaultPassword))
            errors.Add("Password is required.");
        else
            ValidatePassword(row.DefaultPassword, errors);

        if (!string.IsNullOrWhiteSpace(row.PhoneNumber) && !IsValidPhoneNumber(row.PhoneNumber))
            errors.Add($"PhoneNumber '{row.PhoneNumber}' is not valid. Must be 10 digits starting with 0, or in +84 format.");

        if (string.IsNullOrWhiteSpace(row.Section))
        {
            errors.Add("Section is required.");
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
            {
                errors.Add($"Department '{row.Department}' not found.");
                return;
            }

            // Check Department exist in the specified BelongSite
            var site = belongSites.FirstOrDefault(b =>
                b.Name.Equals(row.BelongSite, StringComparison.OrdinalIgnoreCase));

            if (site is null)
            {
                // BelongSite validation already handled above, just stop here
                return;
            }

            if (dept.BelongSiteId != site.Id)
            {
                errors.Add($"Department '{row.Department}' does not belong to BelongSite '{row.BelongSite}'.");
                return;
            }

            // Section must be exist in the specified Department
            if (!string.IsNullOrWhiteSpace(row.Section))
            {
                var sec = sections.FirstOrDefault(s =>
                    (s.EngName.Equals(row.Section, StringComparison.OrdinalIgnoreCase) || s.Name.Equals(row.Section, StringComparison.OrdinalIgnoreCase))
                    && s.DepartmentId == dept.Id);

                if (sec is null)
                    errors.Add($"Section '{row.Section}' not found in Department '{row.Department}'.");
            }
        }


    }

    private static void ValidatePassword(string password, List<string> errors)
    {
        // Identity password policy: RequireDigit, RequireLowercase, RequireUppercase, RequiredLength=6
        if (password.Length < 6)
            errors.Add("Password must be at least 6 characters.");
        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit (0-9).");
        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter (a-z).");
        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter (A-Z).");
    }

    // ── Process ──────────────────────────────────────────────────────────────

    private async Task<string> ProcessRowAsync(
        UserImportRow row,
        IReadOnlyList<DepartmentLookup> departments,
        IReadOnlyList<SectionLookup> sections,
        IReadOnlyList<BelongSiteLookup> belongSites)
    {
        switch (row.Action)
        {
            case ImportAction.Create:
                return await ProcessCreateAsync(row, departments, sections, belongSites);
            case ImportAction.Update:
                return await ProcessUpdateAsync(row, departments, sections, belongSites);
            case ImportAction.Delete:
                return await ProcessDeleteAsync(row);
            default:
                return "Unknown action.";
        }
    }

    private async Task<string> ProcessCreateAsync(
        UserImportRow row,
        IReadOnlyList<DepartmentLookup> departments,
        IReadOnlyList<SectionLookup> sections,
        IReadOnlyList<BelongSiteLookup> belongSites)
    {
        var dept = departments.First(d =>
            d.Name.Equals(row.Department, StringComparison.OrdinalIgnoreCase));
        var sec = sections.First(s =>
            (s.EngName.Equals(row.Section, StringComparison.OrdinalIgnoreCase) || s.Name.Equals(row.Section, StringComparison.OrdinalIgnoreCase))
            && s.DepartmentId == dept.Id);
        var site = belongSites.First(b =>
            b.Name.Equals(row.BelongSite, StringComparison.OrdinalIgnoreCase));

        var appUser = new RegisterUserRequestDto
        {
            UserName = row.UserName,
            EmpId = row.EmpId,
            Email = row.Email,
            FullName = row.FullName!,
            Password = row.DefaultPassword,
            SectionId = sec.Id,
            BelongSiteId = site.Id,
            Position = row.Position,
            DateOfBirth = ParseDate(row.DateOfBirth),
            PhoneNumber = row.PhoneNumber
        };

        var identityResult = await _accountService.RegisterAsync(appUser, null);
        if (!identityResult.Succeeded)
            return $"Failed to create user: {string.Join("; ", identityResult.Errors.Select(e => e.Description))}";

        return null;
    }

    private async Task<string> ProcessUpdateAsync(
        UserImportRow row,
        IReadOnlyList<DepartmentLookup> departments,
        IReadOnlyList<SectionLookup> sections,
        IReadOnlyList<BelongSiteLookup> belongSites)
    {
        var dept = departments.First(d =>
            d.Name.Equals(row.Department, StringComparison.OrdinalIgnoreCase));
        var sec = sections.First(s =>
            (s.EngName.Equals(row.Section, StringComparison.OrdinalIgnoreCase) || s.Name.Equals(row.Section, StringComparison.OrdinalIgnoreCase))
            && s.DepartmentId == dept.Id);
        var site = belongSites.First(b =>
            b.Name.Equals(row.BelongSite, StringComparison.OrdinalIgnoreCase));

        var request = new UpdateUserRequestDto
        {
            UserName = row.UserName,
            EmpId = row.EmpId,
            FullName = row.FullName,
            Email = row.Email,
            SectionId = sec.Id,
            BelongSiteId = site.Id,
            Position = row.Position,
            Password = row.DefaultPassword,
            DateOfBirth = ParseDate(row.DateOfBirth),
            PhoneNumber = row.PhoneNumber
        };

        var updateResult = await _accountService.UpdateAsync(request);
        if (!updateResult.Succeeded)
            return $"Failed to update user: {string.Join("; ", updateResult.Errors.Select(e => e.Description))}";

        return null;
    }

    private async Task<string> ProcessDeleteAsync(UserImportRow row)
    {
        var deactivateResult = await _accountService.SetActiveStatusAsync(row.UserName, false);
        if (!deactivateResult.Succeeded)
            return $"Failed to deactivate user: {string.Join("; ", deactivateResult.Errors.Select(e => e.Description))}";

        return null;
    }



    private static string GetCellValueAsDobString(OfficeOpenXml.ExcelRange cell)
    {
        var v = cell.Value;
        if (v is null) return null;

        if (v is DateTime dt)
            return dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        if (v is double d)
        {
            if (TryOaDateString(d, out var s)) return s;
        }

        // Some Excel/runtime builds return serial as int/long/decimal
        if (v is int or long or decimal)
        {
            try
            {
                if (TryOaDateString(Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture), out var s))
                    return s;
            }
            catch (OverflowException) { }
        }

        return v.ToString()?.Trim();
    }

    private static bool TryOaDateString(double oa, out string s)
    {
        s = null;
        try
        {
            s = DateTime.FromOADate(oa).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var dateInv))
            return dateInv.Date;
        if (DateTime.TryParse(value, out var date))
            return date.Date;
        // dd/MM/yyyy, MM/dd/yyyy, yyyy-MM-dd without culture ambiguity
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "M/d/yyyy" };
        if (DateTime.TryParseExact(value.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var exact))
            return exact.Date;
        // Excel serial as plain text, e.g. "44927"
        if (double.TryParse(value.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var oa))
        {
            try
            {
                return DateTime.FromOADate(oa).Date;
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    private static bool IsValidPhoneNumber(string phone)
    {
        // Accepts: 10 digits starting with 0 (e.g. 0912345678)
        //          +84 followed by 9 digits (e.g. +84912345678)
        return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(0\d{9}|\+84\d{9})$");
    }

    private static bool IsValidEmail(string email)
    {
        try { _ = new System.Net.Mail.MailAddress(email); return true; }
        catch { return false; }
    }

    private sealed class UserExportRow
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string EmpId { get; set; }
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? BelongSiteId { get; set; }
        public string BelongSiteName { get; set; }
        public string Position { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsActive { get; set; }
    }
}
