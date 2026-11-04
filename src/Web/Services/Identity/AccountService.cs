using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Web.Common;
using Web.Common.UploadFileHelper;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Models.Dtos.Admin;
using Web.Models.Dtos.Identity;
using Web.Models.ViewModels.Identity;
using Web.Services.Account;
using Web.Services.UploadFile;

namespace Identity.Services;

public class AccountService : IAccountService
{
    /// <summary>Tất cả avatar user lưu chung một thư mục (không tách theo userId).</summary>
    private static readonly string AvatarFolderPath = "user/avatars";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IUploadFileService _uploadFileService;

    private readonly IAccountNotificationService _accountNotificationService;

    private readonly ILogger<AccountService> _logger;

    private readonly string _baseUrl;

    public AccountService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ApplicationDbContext dbContext,
        IUploadFileService uploadFileService,
        IAccountNotificationService accountNotificationService,
        ILogger<AccountService> logger,
        IConfiguration configuration
        )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _logger = logger;
        _uploadFileService = uploadFileService;
        _accountNotificationService = accountNotificationService;
        _baseUrl = configuration.GetValue<string>("BaseUrl")?.TrimEnd('/') ?? string.Empty;
    }


    public async Task<DataResult<LoginResultDto>> LoginAsync(string userName, string password, bool rememberMe)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return DataResult<LoginResultDto>.Failure("Account not found.");

        if (!user.IsActive)
            return DataResult<LoginResultDto>.Failure("Account is inactive. Please contact IT Admin.");

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!passwordCheck.Succeeded)
            return DataResult<LoginResultDto>.Failure("Password is incorrect.");

        // Check first-time login via claims
        var claims = await _userManager.GetClaimsAsync(user);
        var firstLoginClaim = claims.FirstOrDefault(c => c.Type == "IsFirstTimeLogin");

        bool requireChangePassword;
        if (firstLoginClaim is null)
        {
            await _userManager.AddClaimAsync(user, new Claim("IsFirstTimeLogin", "true"));
            requireChangePassword = true;
        }
        else
        {
            requireChangePassword = string.Equals(firstLoginClaim.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        if (!requireChangePassword)
        {
            await _signInManager.SignInAsync(user, isPersistent: rememberMe);
        }

        return DataResult<LoginResultDto>.Success(new LoginResultDto
        {
            RequireChangePassword = requireChangePassword,
            UserName = user.UserName!
        });
    }

    public async Task<IdentityResult> RegisterAsync(RegisterUserRequestDto request, IFormFile avatar)
    {
        var user = new AppUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email?.Trim(),
            FullName = request.FullName.Trim(),
            EmpId = request.EmpId?.Trim(),
            SectionId = request.SectionId,
            SiteId = request.BelongSiteId,
            Position = request.Position?.Trim(),
            DateOfBirth = request.DateOfBirth,
            PhoneNumber = request.PhoneNumber?.Trim(),
            CreatedAt = DateTime.Now
        };

        string uploadedRelativePath = null;
        if (avatar is { Length: > 0 })
        {
            var upload = await _uploadFileService.UploadFileAsync(avatar, AvatarFolderPath, UploadAllowedExtensions.UserAvatarImage);
            uploadedRelativePath = upload.RelativePath;
            user.Avatar = upload.RelativePath;
        }

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded && uploadedRelativePath != null)
            await _uploadFileService.DeleteFileByRelativePathAsync(uploadedRelativePath);

        return result;
    }

    public async Task<IdentityResult> UpdateAsync(UpdateUserRequestDto request, IFormFile avatar = null, string principalUserName = null)
    {
        var user = await _userManager.FindByNameAsync(request.UserName.Trim());
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = $"User '{request.UserName}' not found." });

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        // SetEmailAsync/SetPhoneNumberAsync (Identity 10) đổi SecurityStamp — chỉ gọi khi contact thật sự đổi.
        // Đổi quyền: UserRoleService.UpdateSecurityStampAsync (phiên cũ hết hiệu lực — LoginSessionNotice).
        var securityStampTouchedByContact = false;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var trimmedEmail = request.Email.Trim();
            if (!string.Equals(trimmedEmail, user.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, trimmedEmail);
                if (!setEmailResult.Succeeded)
                    return setEmailResult;
                securityStampTouchedByContact = true;
            }
        }

        if (request.SectionId.HasValue)
            user.SectionId = request.SectionId.Value;

        if (request.BelongSiteId.HasValue)
            user.SiteId = request.BelongSiteId.Value;

        if (!string.IsNullOrWhiteSpace(request.Position))
            user.Position = request.Position.Trim();

        if (request.DateOfBirth.HasValue)
            user.DateOfBirth = request.DateOfBirth;

        if (!string.IsNullOrWhiteSpace(request.EmpId))
            user.EmpId = request.EmpId.Trim();

        var requestPhone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();
        var currentPhone = string.IsNullOrWhiteSpace(user.PhoneNumber)
            ? null
            : user.PhoneNumber.Trim();
        if (!string.Equals(requestPhone, currentPhone, StringComparison.Ordinal))
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, requestPhone ?? string.Empty);
            if (!setPhoneResult.Succeeded)
                return setPhoneResult;
            securityStampTouchedByContact = true;
        }

        string newAvatarRelativePath = null;
        var previousAvatarRelativePath = user.Avatar;
        if (avatar is { Length: > 0 })
        {
            var upload = await _uploadFileService.UploadFileAsync(avatar, AvatarFolderPath, UploadAllowedExtensions.UserAvatarImage);
            newAvatarRelativePath = upload.RelativePath;
            user.Avatar = newAvatarRelativePath;
        }

        user.UpdatedAt = DateTime.Now;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded && newAvatarRelativePath != null)
        {
            await _uploadFileService.DeleteFileByRelativePathAsync(newAvatarRelativePath);
            return updateResult;
        }

        if (updateResult.Succeeded && newAvatarRelativePath != null
            && !string.IsNullOrWhiteSpace(previousAvatarRelativePath)
            && !string.Equals(previousAvatarRelativePath, newAvatarRelativePath, StringComparison.Ordinal))
        {
            await _uploadFileService.DeleteFileByRelativePathAsync(previousAvatarRelativePath);
        }

        if (updateResult.Succeeded
            && securityStampTouchedByContact
            && !string.IsNullOrWhiteSpace(principalUserName)
            && string.Equals(principalUserName, request.UserName, StringComparison.OrdinalIgnoreCase))
        {
            var refreshed = await _userManager.FindByNameAsync(request.UserName);
            if (refreshed != null)
                await _signInManager.RefreshSignInAsync(refreshed);
        }

        return updateResult;
    }

    /// <summary>
    /// Sets the active status of the specified user.
    ///  If isActive is true, the user will be activated; if false, the user will be deactivated.
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public async Task<IdentityResult> SetActiveStatusAsync(string userName, bool isActive)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = $"User '{userName}' not found." });

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.Now;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded) await _userManager.UpdateSecurityStampAsync(user);
        return result;
    }


    /// <summary>
    /// Resets the password for the specified user. 
    /// If the user does not have a password, it will set the new password directly.
    ///  If the user has an existing password, it will generate a password reset token and reset the password using that token.
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<DataResult<string>> ResetPasswordAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return DataResult<string>.Failure($"User '{userName}' not found.");

        var newPassword = GenerateRandomPassword();

        var newHashedPassword = _userManager.PasswordHasher.HashPassword(user, newPassword);

        user.UpdatedAt = DateTime.Now;
        user.PasswordHash = newHashedPassword;

        var result = await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync();
        if (result.Succeeded) await _userManager.UpdateSecurityStampAsync(user);

        // Mark as first-time login so user must change password
        var claims = await _userManager.GetClaimsAsync(user);
        var firstLoginClaim = claims.FirstOrDefault(c => c.Type == "IsFirstTimeLogin");
        if (firstLoginClaim is not null)
        {
            await _userManager.ReplaceClaimAsync(user, firstLoginClaim, new Claim("IsFirstTimeLogin", "true"));
        }
        else
        {
            await _userManager.AddClaimAsync(user, new Claim("IsFirstTimeLogin", "true"));
        }

        return DataResult<string>.Success(newPassword);
    }

    private static string GenerateRandomPassword()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz!@#$*";
        var stringChars = new char[12];
        var random = new Random();
        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }

    public async Task<DataResult> ChangePasswordAsync(string userName, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return DataResult.Failure("Account not found.");
        if (!user.IsActive || await _userManager.IsLockedOutAsync(user))
            return DataResult.Failure("Tài khoản đã bị khóa.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return DataResult.Failure(result.Errors.Select(e => e.Description));

        // Clear first-time login claim
        var claims = await _userManager.GetClaimsAsync(user);
        var firstLoginClaim = claims.FirstOrDefault(c => c.Type == "IsFirstTimeLogin");
        if (firstLoginClaim is not null)
        {
            await _userManager.ReplaceClaimAsync(user, firstLoginClaim, new Claim("IsFirstTimeLogin", "false"));
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return DataResult.Success();
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<DataResult<UserDetailVM>> GetUserDetail(string userName)
    {
        try
        {
            var user = await _dbContext.AppUsers
            .Where(u => u.UserName == userName)
            .AsNoTracking()
            .Include(user => user.Section)
                .ThenInclude(section => section.Department)
            .Include(user => user.BelongSite)
            .Select(user => new UserDetailVM
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                EmpId = user.EmpId,
                SectionId = user.SectionId,
                SectionName = user.Section.Name,
                DepartmentId = user.Section.DepartmentId,
                DepartmentName = user.Section.Department.Name,
                BelongSiteId = user.SiteId,
                BelongSiteName = user.BelongSite.Name,
                Position = user.Position,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                Avatar = user.Avatar,
                IsOnline = user.IsOnline,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
            }).FirstOrDefaultAsync() ?? throw new Exception("Not found user");
            return DataResult<UserDetailVM>.Success(user, "Success");
        }
        catch (Exception)
        {
            return DataResult<UserDetailVM>.Failure("Error when get detail user");
        }
    }

    public async Task ForgotPassword(ForgotPasswordViewModel request)
    {
        var user = await _dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            throw new InvalidOperationException("User with the provided email does not exist.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = $"{_baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";


        string refreshUrl = $"{_baseUrl}/Account/ForgotPassword";

        // Send reset password email
        await _accountNotificationService.SendResetPasswordEmailAsync(user.Email, user.FullName, callbackUrl, refreshUrl);

    }

    public async Task ResetPassword(ResetPasswordViewModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            throw new InvalidOperationException("User with the provided email does not exist.");

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    /// <summary>
    /// Validate the user request for creating/updating user, such as checking if the assigned section and site exist in the system.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public async Task ValidateUserRequestAsync(RegisterUserRequestDto request)
    {
        if (!request.SectionId.HasValue || !request.BelongSiteId.HasValue)
            throw new InvalidDataException("Mỗi tài khoản phải thuộc một đơn vị (thông qua bộ phận).");
        if (request.SectionId.HasValue)
        {
            var sectionExists = await _dbContext.Sections.AnyAsync(s => s.Id == request.SectionId &&
                s.SiteId == request.BelongSiteId && s.IsActive && s.Department.IsActive);
            if (!sectionExists)
                throw new InvalidDataException($"Wrong assignment user section: Section with Id {request.SectionId} not found.");
        }

        if (request.BelongSiteId.HasValue)
        {
            var siteExists = await _dbContext.BelongSites.AnyAsync(s => s.Id == request.BelongSiteId);
            if (!siteExists) throw new InvalidDataException($"Wrong assignment user site: BelongSite with Id {request.BelongSiteId} not found.");
        }

        // Check duplicate EmpId
        if (!string.IsNullOrWhiteSpace(request.EmpId))
        {
            var empId = request.EmpId.Trim();
            var normalizedEmpId = empId.ToUpper();
            var userName = request.UserName?.Trim();
            var normalizedUserName = userName?.ToUpper();
            var empIdExists = await _dbContext.AppUsers
                .AsNoTracking()
                .AnyAsync(u =>
                    u.EmpId != null &&
                    u.EmpId.Trim().ToUpper() == normalizedEmpId &&
                    (string.IsNullOrWhiteSpace(normalizedUserName) ||
                     u.UserName == null ||
                     u.UserName.ToUpper() != normalizedUserName));

            if (empIdExists)
                throw new InvalidDataException($"EmpId '{empId}' already exists.");
        }
    }
}
