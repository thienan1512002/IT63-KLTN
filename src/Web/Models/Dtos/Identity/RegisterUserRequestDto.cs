using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Identity;

public class RegisterUserRequestDto
{
    // UserName is unique
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public int? SectionId { get; set; } = null;
    public int? BelongSiteId { get; set; } = null;

    // EmpId is unique
    public string EmpId { get; set; } = string.Empty;
    public string Position { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string PhoneNumber { get; set; }
}
