using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Admin;

public class UpdateSectionRequestDto : CreateSectionRequestDto
{
    public int Id { get; set; }
}
