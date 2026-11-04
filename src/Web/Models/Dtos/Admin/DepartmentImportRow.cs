using Web.Models.Enums;

namespace Web.Models.Dtos.Admin;

public class DepartmentImportRow
{
    public int RowNumber { get; set; }
    public ImportAction Action { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; }
    public string Description { get; set; }
    public string BelongSite { get; set; }
    public string MainApprover { get; set; }
    public string BackupApprover { get; set; }
}
