using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Admin;

public class CreateBelongSiteRequestDto
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Code { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; }
}
