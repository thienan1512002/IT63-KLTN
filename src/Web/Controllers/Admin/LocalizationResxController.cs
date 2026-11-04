using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;
using Web.Services.Localization;

namespace Web.Controllers.Admin;

[Authorize(Roles = RoleConstants.SuperUser)]
public class LocalizationResxController : Controller
{
    private readonly ILocalizationResxExcelService _resx;

    public LocalizationResxController(ILocalizationResxExcelService resx) => _resx = resx;

    [HttpGet]
    [Route("api/i18n/export")]
    public IActionResult Export()
    {
        var bytes = _resx.ExportToExcel();
        var fileName = $"i18n-translations-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [IgnoreAntiforgeryToken]
    [Route("api/i18n/import")]
    public async Task<IActionResult> Import([FromForm] IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file or empty file.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Use .xlsx (Excel) export from this app.");

        var result = await _resx.ImportFromExcelAsync(file, cancellationToken);
        return Ok(new
        {
            result.RowsRead,
            result.KeysUpdated,
            result.KeysDeleted,
            Warnings = result.Warnings,
            result.FilesWritten
        });
    }

    [HttpGet]
    [Route("api/i18n/import2")]
    [AllowAnonymous]
    public async Task<IActionResult> Import2(CancellationToken cancellationToken = default)
    {
        // Get file from wwwroot/xlsx_template/i18n-translations.xlsx
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "xlsx_template", "i18n-translations.xlsx");
        if (!System.IO.File.Exists(filePath))
            return NotFound("Template file not found.");
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        // convert to IFormFile
        var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", Path.GetFileName(filePath));
        var result = await _resx.ImportFromExcelAsync(formFile, cancellationToken);
        return Ok(new
        {
            result.RowsRead,
            result.KeysUpdated,
            result.KeysDeleted,
            Warnings = result.Warnings,
            result.FilesWritten
        });
    }
}
