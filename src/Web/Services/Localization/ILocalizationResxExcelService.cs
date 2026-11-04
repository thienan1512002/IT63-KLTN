namespace Web.Services.Localization;

public interface ILocalizationResxExcelService
{
    /// <summary>Build a workbook: title rows 1–2, headers row 3 (Key, Default, en-US, vi-VN, Action). Action D on import removes the key.</summary>
    byte[] ExportToExcel();

    /// <summary>Update .resx from workbook. Header row is auto-detected. Empty value = skip. Action D = remove key from all culture resx. Rebuild after import.</summary>
    Task<LocalizationResxImportResult> ImportFromExcelAsync(IFormFile file, CancellationToken cancellationToken = default);
}
