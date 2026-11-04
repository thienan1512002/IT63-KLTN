using System.Drawing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Web.Services.Localization;

public class LocalizationResxExcelService : ILocalizationResxExcelService
{
    private const string HKey = "Key";
    private const string HDefault = "Default";
    private const string HEn = "en-US";
    private const string HVi = "vi-VN";
    private const string HAction = "Action";
    private const string LegacySheetName = "SharedResource";

    private const int LastDataColumn = 5;
    private const int NoteCol = 7;

    private static readonly ResourceSheetDefinition[] ResourceSheets =
    [
        new("SharedResource", "Application Localization Resource Sheet", "SharedResource.resx", "SharedResource.en-US.resx", "SharedResource.vi-VN.resx")
    ];

    private readonly string _resourcesDir;

    public LocalizationResxExcelService(IWebHostEnvironment env)
    {
        _resourcesDir = Path.Combine(env.ContentRootPath, "Resources");
    }

    public byte[] ExportToExcel()
    {
        using var package = new ExcelPackage();
        foreach (var sheet in ResourceSheets)
            ExportSheet(package, sheet);

        return package.GetAsByteArray();
    }

    private static void ApplyExportThinBorder(ExcelRange range)
    {
        var b = range.Style.Border;
        b.Top.Style = ExcelBorderStyle.Thin;
        b.Left.Style = ExcelBorderStyle.Thin;
        b.Bottom.Style = ExcelBorderStyle.Thin;
        b.Right.Style = ExcelBorderStyle.Thin;
    }

    public Task<LocalizationResxImportResult> ImportFromExcelAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("File is required.", nameof(file));

        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        if (package.Workbook.Worksheets.Count < 1)
            throw new InvalidOperationException("Workbook has no worksheets.");
        var warnings = new List<string>();
        var written = 0;
        var keysDeleted = 0;
        var rows = 0;
        var writtenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ws in package.Workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ws.Dimension is null)
            {
                warnings.Add($"Worksheet '{ws.Name}' is empty.");
                continue;
            }

            var sheet = ResolveSheetDefinition(ws.Name);
            var result = ImportSheet(ws, sheet, cancellationToken);
            rows += result.RowsRead;
            written += result.KeysUpdated;
            keysDeleted += result.KeysDeleted;
            foreach (var warning in result.Warnings)
                warnings.Add(warning);
            foreach (var path in result.FilesWritten)
                writtenPaths.Add(path);
        }

        if (rows == 0 && warnings.Count == 0)
            warnings.Add("No data rows (keys) were processed.");

        return Task.FromResult(new LocalizationResxImportResult
        {
            RowsRead = rows,
            KeysUpdated = written,
            KeysDeleted = keysDeleted,
            Warnings = warnings,
            FilesWritten = writtenPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList()
        });
    }

    private void ExportSheet(ExcelPackage package, ResourceSheetDefinition sheet)
    {
        var d0 = ResxFileHelper.ReadResxData(Path.Combine(_resourcesDir, sheet.DefaultFile));
        var dEn = ResxFileHelper.ReadResxData(Path.Combine(_resourcesDir, sheet.EnUsFile));
        var dVi = ResxFileHelper.ReadResxData(Path.Combine(_resourcesDir, sheet.ViVnFile));

        var allKeys = d0.Keys.Union(dEn.Keys).Union(dVi.Keys)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        var ws = package.Workbook.Worksheets.Add(sheet.SheetName);
        var orangeColor = Color.FromArgb(255, 178, 102);

        var title = ws.Cells[1, 1, 2, LastDataColumn];
        title.Merge = true;
        title.Value = sheet.Title;
        title.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        title.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        title.Style.Font.Bold = true;
        title.Style.Font.Size = 14;
        ApplyExportThinBorder(title);
        ws.Row(1).Height = 24;
        ws.Row(2).Height = 24;

        var noteR1 = ws.Cells[1, NoteCol];
        noteR1.Value = null;
        noteR1.Style.Fill.PatternType = ExcelFillStyle.Solid;
        noteR1.Style.Fill.BackgroundColor.SetColor(orangeColor);
        ApplyExportThinBorder(noteR1);
        noteR1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        noteR1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        var noteR2 = ws.Cells[2, NoteCol];
        noteR2.Value = "Action D: Delete key";
        noteR2.Style.Font.Bold = true;
        noteR2.Style.Font.Color.SetColor(Color.FromArgb(32, 32, 32));
        noteR2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        noteR2.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        noteR2.Style.WrapText = true;
        noteR2.Style.Fill.PatternType = ExcelFillStyle.Solid;
        noteR2.Style.Fill.BackgroundColor.SetColor(Color.Transparent);
        ApplyExportThinBorder(noteR2);

        const int headerRow = 3;
        ws.Cells[headerRow, 1].Value = HKey;
        ws.Cells[headerRow, 2].Value = HDefault;
        ws.Cells[headerRow, 3].Value = HEn;
        ws.Cells[headerRow, 4].Value = HVi;
        ws.Cells[headerRow, 5].Value = HAction;

        for (var c = 1; c <= LastDataColumn; c++)
        {
            var head = ws.Cells[headerRow, c];
            head.Style.Font.Bold = true;
            head.Style.Fill.PatternType = ExcelFillStyle.Solid;
            head.Style.Fill.BackgroundColor.SetColor(orangeColor);
            head.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            head.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyExportThinBorder(head);
        }

        var row = headerRow + 1;
        foreach (var key in allKeys)
        {
            ws.Cells[row, 1].Value = key;
            if (d0.TryGetValue(key, out var v0)) ws.Cells[row, 2].Value = v0;
            if (dEn.TryGetValue(key, out var vEn)) ws.Cells[row, 3].Value = vEn;
            if (dVi.TryGetValue(key, out var vVi)) ws.Cells[row, 4].Value = vVi;
            row++;
        }

        for (var i = 1; i <= LastDataColumn; i++)
        {
            var excelCol = ws.Column(i);
            excelCol.Width = 44;
            excelCol.Style.WrapText = true;
        }

        ws.Column(NoteCol).Width = 22;
        ws.Column(LastDataColumn).Width = 10;
    }

    private LocalizationResxImportResult ImportSheet(
        ExcelWorksheet ws,
        ResourceSheetDefinition sheet,
        CancellationToken cancellationToken)
    {
        var pathDefault = Path.Combine(_resourcesDir, sheet.DefaultFile);
        var pathEn = Path.Combine(_resourcesDir, sheet.EnUsFile);
        var pathVi = Path.Combine(_resourcesDir, sheet.ViVnFile);

        var endRow = ws.Dimension!.End.Row;
        var endCol = ws.Dimension.End.Column;
        var headerRow = FindHeaderRow(ws, endRow, endCol);
        if (headerRow < 1)
            throw new InvalidOperationException(
                $"Worksheet '{ws.Name}' could not find a header row with columns Key and (Default or en-US or vi-VN or Action).");

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var c = 1; c <= endCol; c++)
        {
            var h = (ws.Cells[headerRow, c].Value as string) ?? ws.Cells[headerRow, c].Value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(h)) continue;
            headers[h] = c;
        }

        if (!headers.TryGetValue(HKey, out var colKey))
            throw new InvalidOperationException(
                $"Worksheet '{ws.Name}' must include a column named '{HKey}'. Found: {string.Join(", ", headers.Keys)}");

        int? cDef = FindColumn(headers, HDefault);
        int? cEn = FindColumn(headers, HEn);
        int? cVi = FindColumn(headers, HVi);
        int? cAction = FindColumn(headers, HAction);

        if (cDef is null && cEn is null && cVi is null && cAction is null)
            throw new InvalidOperationException(
                $"Worksheet '{ws.Name}' must include at least one of: {HDefault}, {HEn}, {HVi}, or {HAction}.");

        var docDefault = ResxFileHelper.LoadOrCreate(pathDefault);
        var docEn = ResxFileHelper.LoadOrCreate(pathEn);
        var docVi = ResxFileHelper.LoadOrCreate(pathVi);

        var warnings = new List<string>();
        var written = 0;
        var keysDeleted = 0;
        var rows = 0;

        var dataStart = headerRow + 1;
        for (var r = dataStart; r <= endRow; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (ws.Cells[r, colKey].Value as string) ?? ws.Cells[r, colKey].Value?.ToString() ?? string.Empty;
            key = key.Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            rows++;

            if (cAction is { } iAct && IsDeleteAction(ws, r, iAct))
            {
                ResxFileHelper.RemoveDataKey(docDefault, key);
                ResxFileHelper.RemoveDataKey(docEn, key);
                ResxFileHelper.RemoveDataKey(docVi, key);
                keysDeleted++;
                continue;
            }

            if (cDef is { } iDef)
            {
                var t = GetCellString(ws, r, iDef);
                if (t is not null)
                {
                    ResxFileHelper.UpsertDataValue(docDefault, key, t);
                    written++;
                }
            }
            if (cEn is { } iEn)
            {
                var t = GetCellString(ws, r, iEn);
                if (t is not null)
                {
                    ResxFileHelper.UpsertDataValue(docEn, key, t);
                    written++;
                }
            }
            if (cVi is { } iVi)
            {
                var t = GetCellString(ws, r, iVi);
                if (t is not null)
                {
                    ResxFileHelper.UpsertDataValue(docVi, key, t);
                    written++;
                }
            }
        }

        if (rows == 0)
            warnings.Add($"Worksheet '{ws.Name}' has no data rows (keys).");

        var writtenPaths = new List<string>();
        ResxFileHelper.SaveResxWithUtf8Bom(pathDefault, docDefault);
        writtenPaths.Add(pathDefault);
        ResxFileHelper.SaveResxWithUtf8Bom(pathEn, docEn);
        writtenPaths.Add(pathEn);
        ResxFileHelper.SaveResxWithUtf8Bom(pathVi, docVi);
        writtenPaths.Add(pathVi);

        return new LocalizationResxImportResult
        {
            RowsRead = rows,
            KeysUpdated = written,
            KeysDeleted = keysDeleted,
            Warnings = warnings,
            FilesWritten = writtenPaths
        };
    }

    private static ResourceSheetDefinition ResolveSheetDefinition(string sheetName)
    {
        var match = ResourceSheets.FirstOrDefault(sheet =>
            string.Equals(sheet.SheetName, sheetName, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return match;

        if (string.Equals(sheetName, LegacySheetName, StringComparison.OrdinalIgnoreCase))
            return ResourceSheets[0];

        throw new InvalidOperationException($"Worksheet '{sheetName}' is not a supported localization resource set.");
    }

    private static int FindHeaderRow(ExcelWorksheet ws, int endRow, int endCol)
    {
        var last = Math.Min(endRow, 50);
        for (var r = 1; r <= last; r++)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 1; c <= endCol; c++)
            {
                var h = ws.Cells[r, c].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(h)) names.Add(h);
            }

            if (names.Contains(HKey) && (names.Contains(HDefault) || names.Contains(HEn) || names.Contains(HVi) ||
                names.Contains(HAction)))
                return r;
        }
        return 0;
    }

    private static int? FindColumn(Dictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var i) ? i : null;

    private static bool IsDeleteAction(ExcelWorksheet ws, int row, int col)
    {
        var v = ws.Cells[row, col].Value;
        if (v is null) return false;
        var s = (v as string) ?? v.ToString() ?? string.Empty;
        s = s.Trim();
        return s.Length == 1 && s[0] is 'D' or 'd';
    }

    /// <summary>Returns null to skip (blank cell). Otherwise updates .resx for that cell.</summary>
    private static string GetCellString(ExcelWorksheet ws, int row, int col)
    {
        var v = ws.Cells[row, col].Value;
        if (v is null) return null;
        if (v is string s) return s;
        if (v is double or int or long or float or decimal) return v.ToString() ?? string.Empty;
        return v.ToString() ?? string.Empty;
    }

    private sealed record ResourceSheetDefinition(
        string SheetName,
        string Title,
        string DefaultFile,
        string EnUsFile,
        string ViVnFile);
}
