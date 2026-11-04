namespace Web.Models.Dtos.Identity;

public class LoginResultDto
{
    public bool RequireChangePassword { get; set; }
    public string UserName { get; set; } = string.Empty;
}
