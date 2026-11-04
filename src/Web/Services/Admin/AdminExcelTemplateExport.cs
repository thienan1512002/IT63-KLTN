using Microsoft.AspNetCore.Hosting;
using OfficeOpenXml;

namespace Web.Services.Admin;

internal static class AdminExcelTemplateExport
{
    public const int StartRow = 3;

    public static byte[] FillTemplate<T>(
        IWebHostEnvironment env,
        string templateFileName,
        int lastColumn,
        IReadOnlyList<T> rows,
        Action<ExcelWorksheet, int, int, T> writeRow)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var templatePath = Path.Combine(webRoot, "xlsx_template", templateFileName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Excel template '{templateFileName}' was not found.", templatePath);

        using var stream = new MemoryStream(File.ReadAllBytes(templatePath));
        using var package = new ExcelPackage(stream);
        if (package.Workbook.Worksheets.Count < 1)
            throw new InvalidOperationException($"Excel template '{templateFileName}' has no worksheets.");

        var ws = package.Workbook.Worksheets[0];
        ClearExistingData(ws, lastColumn);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = StartRow + i;
            //CopyTemplateRow(ws, row, lastColumn);
            ws.Cells[row, 1].Value = i + 1;
            writeRow(ws, row, i, rows[i]);
        }

        // Apply border for all cells
        ApplyStyle(ws, lastColumn);

        return package.GetAsByteArray();
    }

    private static void ApplyStyle(ExcelWorksheet ws, int lastColumn)
    {
        // Add border for all cells in the template by using range to improve performance
        var endRow = Math.Max(ws.Dimension?.End.Row ?? StartRow, StartRow);
        using (var range = ws.Cells[StartRow, 1, endRow, lastColumn])
        {
            range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;            
        }

    }

    private static void CopyTemplateRow(ExcelWorksheet ws, int row, int lastColumn)
    {
        if (row == StartRow)
            return;

        ws.Cells[StartRow, 1, StartRow, lastColumn]
            .Copy(ws.Cells[row, 1, row, lastColumn]);
        ws.Row(row).Height = ws.Row(StartRow).Height;
    }

    private static void ClearExistingData(ExcelWorksheet ws, int lastColumn)
    {
        var endRow = Math.Max(ws.Dimension?.End.Row ?? StartRow, StartRow);
        for (var row = StartRow; row <= endRow; row++)
        {
            for (var col = 1; col <= lastColumn; col++)
            {
                ws.Cells[row, col].Value = null;
            }
        }
    }
}
