using System.ComponentModel.DataAnnotations;

namespace UserModule.Models.ViewModels;

public class CreateUserViewModel
{   

    [Required]
    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }

    [Phone]
    public string PhoneNumber { get; set; }

    [MaxLength(128)]
    public string Department { get; set; }

    [MaxLength(128)]
    public string Section { get; set; }

    [MaxLength(128)]
    public string Position { get; set; }
}
