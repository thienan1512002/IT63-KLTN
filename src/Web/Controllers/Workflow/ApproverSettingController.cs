using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;

namespace Web.Controllers.Workflow;

[Authorize(Roles = RoleConstants.SuperUser)]
[Route("[controller]")]
public class ApproverSettingController : Controller
{
    public IActionResult Index() => View("~/Views/Workflow/ApproverSetting/Index.cshtml");
}