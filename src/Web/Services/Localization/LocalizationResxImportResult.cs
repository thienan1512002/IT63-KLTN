namespace Web.Services.Localization;

public class LocalizationResxImportResult
{
    public int RowsRead { get; set; }
    /// <summary>Number of cell-level upserts applied (Default / en-US / vi-VN).</summary>
    public int KeysUpdated { get; set; }
    /// <summary>Number of data rows where Action was D and the key was removed from .resx.</summary>
    public int KeysDeleted { get; set; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> FilesWritten { get; init; } = [];
}
