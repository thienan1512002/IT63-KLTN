using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Identity;

public class LoginDto
{

    [Required]    
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
