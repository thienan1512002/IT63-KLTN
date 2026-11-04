using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Admin;

public class CreateSectionRequestDto
{
    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Required]
    public string EngName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Note { get; set; }

    [MaxLength(200)]
    public string ShortName { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public int BelongSiteId { get; set; }

    [MaxLength(255)]
    public string MainApprover { get; set; }

    [MaxLength(255)]
    public string BackupApprover { get; set; }

    [MaxLength(1000)]
    public string PIC { get; set; }

    [MaxLength(1000)]
    public string DeputyApprovers { get; set; }
}
