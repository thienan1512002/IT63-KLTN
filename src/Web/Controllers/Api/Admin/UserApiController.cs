using DevExtreme.AspNet.Data;
using Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Web.Models.Dtos.Admin;
using Web.Models.Dtos.Identity;
using Web.Models.Dtos.User;
using Web.Models.FilterModels.Users;
using Web.Services.Account;
using Web.Services.Admin;
using Web.Services.Localization;

namespace Web.Controllers.Api.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/users")]
    public class UserApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IAccountService _accountService;
        private readonly IImportUserService _importUserService;

        private readonly UserStatusService _userStatusService;
        private readonly ILogger<UserApiController> _logger;
        private readonly UserManager<Web.Data.Entities.Identity.AppUser> _userManager;

        public UserApiController(
            IUserService userService,
            IUserRoleService userRoleService,
            IAccountService accountService,
            IImportUserService importUserService,
            UserStatusService userStatusService,
            ILogger<UserApiController> logger,
            UserManager<Web.Data.Entities.Identity.AppUser> userManager)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _accountService = accountService;
            _importUserService = importUserService;
            _userStatusService = userStatusService;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>Roles grouped by CategoryName for permission UI.</summary>
        [HttpGet("{userId}/roles")]
        [Authorize(Roles = Web.Common.RoleConstants.SuperUser)]
        public async Task<IActionResult> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
        {
            var detail = await _userRoleService.GetUserRolesDetailAsync(userId, cancellationToken);
            if (detail is null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(detail);
        }

        /// <summary>Replace user roles with the given role ids.</summary>
        [HttpPut("{userId}/roles")]
        [Authorize(Roles = Web.Common.RoleConstants.SuperUser)]
        public async Task<IActionResult> UpdateUserRolesAsync(string userId, [FromBody] UpdateUserRolesRequestDto request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest(new { message = "Request body is required." });
            }

            var result = await _userRoleService.UpdateUserRolesAsync(userId, request.RoleIds ?? new List<string>(), cancellationToken);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Failed to update roles.",
                    errors = result.Errors.Select(e => e.Description).ToArray(),
                });
            }

            return Ok(new { message = "Roles updated successfully." });
        }



        [HttpGet]
        public async Task<IActionResult> GetListAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken)
            => await GetAuthorizedListAsync(loadOptions, cancellationToken);

        [NonAction]
        private async Task<IActionResult> GetAuthorizedListAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken)
        {
            if (!Web.Common.RoleConstants.AccessPersonnel().Any(User.IsInRole)) return Forbid();
            var query = _userService.GetListQuery();
            var result = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
            return Ok(result);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookupAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken)
        {
            var query = _userService.GetListQuery()
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.FullName,
                    u.SectionName,
                    u.DepartmentName,
                    u.BelongSiteName,
                    u.Position
                });
            var result = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
        public async Task<IActionResult> CreateAsync([FromForm] RegisterUserRequestDto request, IFormFile avatar = null)
        {
            ValidateCreateRequest(request);
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            try
            {
                await _accountService.ValidateUserRequestAsync(request);

                var result = await _accountService.RegisterAsync(request, avatar);
                if (!result.Succeeded)
                {
                    return BadRequest(CreateErrorResponse(result));
                }

                var createdUser = await _userService.GetByUserNameAsync(request.UserName);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = "User created successfully.",
                    userId = createdUser?.Id,
                    userName = createdUser?.UserName
                });
            }
            catch (Exception ex)
            {
                if (ex is InvalidDataException)
                {
                    return BadRequest(new { message = ex.Message });
                }

                _logger.LogError(ex, "Error creating user with UserName {UserName}", request.UserName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the user.", details = ex.Message });
            }
        }

        [HttpPut("{userId}")]
        [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
        public async Task<IActionResult> UpdateAsync(string userId, [FromForm] UpdateUserRequestDto request, IFormFile avatar = null)
        {
            try
            {
                var user = await _userService.GetByUserNameAsync(userId);
                if (user is null)
                {
                    return NotFound(new { message = "User not found." });
                }
                if (!await CanManageTarget(user)) return Forbid();
                if (!string.Equals(request.UserName, user.UserName, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Tài khoản trong dữ liệu không khớp đường dẫn." });

                await _accountService.ValidateUserRequestAsync(request);

                var result = await _accountService.UpdateAsync(request, avatar, User.Identity?.Name);
                if (!result.Succeeded)
                {
                    return BadRequest(CreateErrorResponse(result));
                }

                return Ok(new { message = LanguageService.Translate("umf_msg_updated") });
            }
            catch (Exception ex)
            {
                if (ex is InvalidDataException)
                {
                    return BadRequest(new { message = ex.Message });
                }

                _logger.LogError(ex, "Error updating user with Id {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the user.", details = ex.Message });
            }
        }

        [HttpPost("{userId}/reset-password")]
        [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
        public async Task<IActionResult> ResetPasswordAsync(string userId)
        {
            var user = await _userService.GetByUserNameAsync(userId);
            if (user is null)
            {
                return NotFound(new { message = "User not found." });
            }
            if (!await CanManageTarget(user)) return Forbid();

            var result = await _accountService.ResetPasswordAsync(user.UserName!);
            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Message, errors = result.Errors });
            }

            return Ok(new { message = "Password reset successfully.", newPassword = result.Data });
        }

        [HttpPatch("{userId}")]
        [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
        public async Task<IActionResult> UpdateStatusAsync(string userId, [FromBody] SetStatusRequestDto request)
        {
            if (request is null)
            {
                return BadRequest(new { message = "Request body is required." });
            }

            var user = await _userService.GetByUserNameAsync(userId);
            if (user is null)
            {
                return NotFound(new { message = "User not found." });
            }
            if (!await CanManageTarget(user)) return Forbid();

            var result = await _accountService.SetActiveStatusAsync(user.UserName!, request.IsActive);
            if (!result.Succeeded)
            {
                return BadRequest(CreateErrorResponse(result));
            }

            var statusMessage = request.IsActive ? "activated" : "deactivated";
            return Ok(new { message = $"Account has been {statusMessage} successfully." });
        }

        private async Task<bool> CanManageTarget(Web.Data.Entities.Identity.AppUser target) =>
            User.IsInRole(Web.Common.RoleConstants.SuperUser) ||
            !await _userManager.IsInRoleAsync(target, Web.Common.RoleConstants.SuperUser);

        private void ValidateCreateRequest(RegisterUserRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                ModelState.AddModelError(nameof(request.UserName), "UserName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                ModelState.AddModelError(nameof(request.FullName), "FullName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                ModelState.AddModelError(nameof(request.Password), "Password is required.");
            }

            if (request.SectionId <= 0)
            {
                ModelState.AddModelError(nameof(request.SectionId), "Section is required.");
            }

            if (request.BelongSiteId <= 0)
            {
                ModelState.AddModelError(nameof(request.BelongSiteId), "BelongSite is required.");
            }

            if (string.IsNullOrWhiteSpace(request.EmpId))
            {
                ModelState.AddModelError(nameof(request.EmpId), "Emp ID is required.");
            }
        }

        [HttpPost("import")]
        [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
        public async Task<IActionResult> ImportAsync(IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "File is required." });

            var result = await _importUserService.ImportUserAsync(file);
            return Ok(result);
        }

        [HttpPost("export")]
        [Authorize(Roles = Web.Common.RoleConstants.ManagePersonnelAuthorizeRoles)]
        public async Task<IActionResult> ExportAsync(
            [FromBody] Dictionary<string, JsonElement> loadOptionsPayload,
            CancellationToken cancellationToken)
        {
            try
            {
                var loadOptions = DataSourceLoadOptions.FromJsonPayload(loadOptionsPayload);
                var bytes = await _importUserService.ExportAsync(loadOptions, cancellationToken);
                var fileName = $"NVU_Users_{DateTime.Now:yyyyMMddHHmm}.xlsx";
                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while exporting users." });
            }
        }

        private static object CreateErrorResponse(IdentityResult result)
        {
            return new
            {
                errors = result.Errors.Select(error => error.Description).ToArray()
            };
        }



        [HttpGet]
        [Authorize]
        [Route("/api/users/detail")]
        public async Task<IActionResult> GetUserDetailAsync([FromQuery] string userName)
        {
            if (userName != User.Identity?.Name && !Web.Common.RoleConstants.ManagePersonnel().Any(User.IsInRole))
                return Forbid();
            return Ok(await _accountService.GetUserDetail(userName));
        }

        [HttpGet]
        [Authorize]
        [Route("/api/users/current-user")]
        public async Task<IActionResult> GetCurrentUserDetailAsync()
        {
            var userName = User.Identity?.Name;
            return Ok(await _accountService.GetUserDetail(userName));
        }

        /// <summary>
        /// Đổi mật khẩu cho user đang đăng nhập (không dùng chung với UpdateAsync — gọi ChangePasswordAsync).
        /// Trang Setting luôn dùng <c>User.Identity.Name</c> nên khớp với tài khoản session.
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("/api/users/change-password")]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDto body)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.NewPassword))
            {
                return BadRequest(new { message = "New password is required." });
            }

            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }

            var result = await _accountService.ChangePasswordAsync(
                userName,
                body.CurrentPassword ?? string.Empty,
                body.NewPassword);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Message ?? "Could not change password." });
            }

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpPut("/api/users/profile")]
        public async Task<IActionResult> UpdateOwnProfile([FromForm] UpdateUserRequestDto request, IFormFile avatar = null)
        {
            var current = await _userService.GetByUserNameAsync(User.Identity.Name);
            if (current == null || !current.IsActive) return Forbid();
            request.UserName = current.UserName;
            request.EmpId = current.EmpId;
            request.SectionId = current.SectionId;
            request.BelongSiteId = current.SiteId;
            var result = await _accountService.UpdateAsync(request, avatar, current.UserName);
            return result.Succeeded ? Ok(new { message = "Đã cập nhật hồ sơ." }) : BadRequest(CreateErrorResponse(result));
        }

        [HttpGet("/api/users/details")]
        public async Task<IActionResult> GetUserDetailsAsync([FromQuery] UserFM filter)
        {
            var result = await _userService.GetUserDetailsAsync(filter);
            return Ok(result.Data);
        }

        [HttpPut]
        [Authorize]
        [Route("/api/user/status")]
        public async Task<IActionResult> UpdateUserStatusAsync([FromBody] UpdateUserStatusRequestDto request)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }

            request.UserName = userName;

            try
            {
                await _userStatusService.UpdateUserStatus(request);

            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating user status." });
            }

            var statusMessage = request.GoInactive ? "deactivated" : "activated";
            return Ok(new { message = $"Account has been {statusMessage} successfully." });
        }
    }
}
