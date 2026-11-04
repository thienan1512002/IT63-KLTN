namespace Web.Models.Dtos.Admin;

public class SectionListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EngName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int BelongSiteId { get; set; }
    public string BelongSiteName { get; set; } = string.Empty;
    public string MainApprover { get; set; } = string.Empty;
    public string BackupApprover { get; set; } = string.Empty;
    public string PIC { get; set; } = string.Empty;
    public string DeputyApprovers { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
