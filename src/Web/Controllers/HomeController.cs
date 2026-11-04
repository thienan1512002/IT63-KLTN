using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Web.Services.Workflow;

namespace Host.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult SetLanguage(string culture, string returnUrl = null)
    {
        if (culture is not "en-US" and not "vi-VN")
            culture = "en-US";
        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            returnUrl = Url.Action(nameof(Index)) ?? "/";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
        return LocalRedirect(returnUrl);
    }

    [AllowAnonymous]
    [Route("/Error")]
    public IActionResult Error() => View();

    [AllowAnonymous]
    [Route("/NotAccess")]
    public IActionResult NotAccess() => View("NotAccessV2");
}
