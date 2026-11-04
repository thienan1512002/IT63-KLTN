using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Web.Common;
using Web.Models.Dtos.Admin;
using Web.Models.Dtos.Identity;
using Web.Models.ViewModels.Identity;

namespace Identity.Services;

public interface IAccountService
{
    Task<DataResult<LoginResultDto>> LoginAsync(string userName, string password, bool rememberMe);
    Task<IdentityResult> RegisterAsync(RegisterUserRequestDto request, IFormFile avatar);
    Task<IdentityResult> UpdateAsync(UpdateUserRequestDto request, IFormFile avatar = null, string principalUserName = null);
    Task<IdentityResult> SetActiveStatusAsync(string userName, bool isActive);
    Task<DataResult<string>> ResetPasswordAsync(string userName);
    Task<DataResult> ChangePasswordAsync(string userName, string currentPassword, string newPassword);
    Task LogoutAsync();
    Task<DataResult<UserDetailVM>> GetUserDetail(string userName);

    Task ForgotPassword(ForgotPasswordViewModel request);

    Task ResetPassword(ResetPasswordViewModel model);

    Task ValidateUserRequestAsync(RegisterUserRequestDto request);
}
