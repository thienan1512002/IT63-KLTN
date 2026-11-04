using Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Data.Entities.Identity;
using Web.Models.Dtos.Identity;
using Web.Models.ViewModels.Identity;

namespace Web.Controllers.Identity;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    /// <summary>TempData key: hiện Swal sinh nhật lần đầu vào Home sau khi đăng nhập (không giới hạn 1 lần/ngày).</summary>
    public const string TempDataShowBirthdayOnHome = "ShowBirthdaySwalOnHome";

    public AccountController(IAccountService accountService, UserManager<AppUser> userManager, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _userManager = userManager;
        _logger = logger;
    }

    private void SetBirthdayTempDataIfToday(AppUser user)
    {
        if (user == null || user.DateOfBirth is not { } dob)
            return;
        var t = DateTime.Today;
        if (dob.Month == t.Month && dob.Day == t.Day)
            TempData[TempDataShowBirthdayOnHome] = true;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpGet]
    public IActionResult UpdatePassword(string userName)
    {
        var model = new UpdatePasswordViewModel { UserName = userName };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(UpdatePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _accountService.ChangePasswordAsync(model.UserName, model.CurrentPass, model.NewPass);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Failed to update password.");
            return View(model);
        }

        var userAfterPw = await _userManager.FindByNameAsync(model.UserName);
        SetBirthdayTempDataIfToday(userAfterPw);

        return RedirectToAction("Index", "Home");
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _accountService.LoginAsync(
            model.UserName,
            model.Password,
            model.RememberMe);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        if (result.Data!.RequireChangePassword)
        {
            return RedirectToAction("UpdatePassword", new { userName = result.Data.UserName });
        }

        var loggedInUser = await _userManager.FindByNameAsync(model.UserName);
        SetBirthdayTempDataIfToday(loggedInUser);

        return LocalRedirect(model.ReturnUrl ?? "/");
    }




    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpGet]
    public Task<IActionResult> ResetPassword(string token, string email)
    {
        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return Task.FromResult<IActionResult>(View(model));
    }

    /// <summary>
    /// Resets the password for a user that received a password reset email. This action is typically accessed via a link in the email sent to the user.
    /// The user must provide a new password and confirm it. The token from the email is used to verify the request.
    /// </summary>
    /// <param name="resetPasswordModel"></param>
    /// <returns></returns>

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordModel)
    {
        if (!ModelState.IsValid)
        {
            return View(resetPasswordModel);
        }
        try
        {
            await _accountService.ResetPassword(resetPasswordModel);
        }
        catch (Exception ex)
        {
            // Log the exception if needed
            _logger.LogError(ex, "Error in ResetPassword");

            // Optionally, you can add a generic error message to the ModelState
            ModelState.AddModelError(string.Empty, "An error occurred while processing your request. Please try again later.");
            return View(resetPasswordModel);
        }

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _accountService.ForgotPassword(model);
        }
        catch (Exception ex)
        {
            // Log the exception if needed
            _logger.LogError(ex, "Error in ForgotPassword");

            // Optionally, you can add a generic error message to the ModelState
            ModelState.AddModelError(string.Empty, "An error occurred while processing your request. Please try again later.");
            return View(model);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }


    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
