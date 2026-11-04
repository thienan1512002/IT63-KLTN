using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Admin;

public class UpdateDepartmentRequestDto : CreateDepartmentRequestDto
{
    public int Id { get; set; }
}
