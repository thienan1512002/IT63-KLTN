namespace Web.Models.Dtos.Identity;

/// <summary>Đổi mật khẩu (user hiện tại — so khớp với session).</summary>
public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
