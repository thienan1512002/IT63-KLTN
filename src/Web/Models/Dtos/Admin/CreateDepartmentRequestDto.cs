using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Admin;

public class CreateDepartmentRequestDto
{
    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string ShortName { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; }

    [Required]
    public int BelongSiteId { get; set; }

    [MaxLength(255)]
    public string MainApprover { get; set; }

    [MaxLength(255)]
    public string BackupApprover { get; set; }
}
