using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;

namespace Web.Controllers.Admin;

public class SectionController : Controller
{
    [HttpGet]
    [Authorize(Roles = RoleConstants.ManagePersonnelAuthorizeRoles)]
    public IActionResult Index()
    {
        ViewData["AllowManage"] = AuthHelper.CheckRoles(User, RoleConstants.ManagePersonnel());
        return View();
    }
}