using Web.Models.Enums;

namespace Web.Models.Dtos.Admin;

public class SectionImportRow
{
    public int RowNumber { get; set; }
    public ImportAction Action { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EngName { get; set; } = string.Empty;
    public string Note { get; set; }
    public string ShortName { get; set; }
    public string Department { get; set; }
    public string BelongSite { get; set; }
    public string MainApprover { get; set; }
    public string BackupApprover { get; set; }
    public string PIC { get; set; }
    public string DeputyApprovers { get; set; }
}
