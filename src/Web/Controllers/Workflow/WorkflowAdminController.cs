using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;

namespace Web.Controllers.Workflow;

[Authorize(Roles = RoleConstants.SuperUser)]
public class WorkflowAdminController : Controller
{
    [HttpGet("RequestTypes")] public IActionResult RequestTypes() => View("~/Views/Workflow/Admin/RequestTypes.cshtml");
    [HttpGet("RequestMasterData")] public IActionResult RequestMasterData() => View("~/Views/Workflow/Admin/RequestMasterData.cshtml");
    [HttpGet("Workflows")] public IActionResult Workflows() => View("~/Views/Workflow/Admin/Workflows.cshtml");
    [HttpGet("Roles")] public IActionResult Roles() => View("~/Views/Admin/Roles/Index.cshtml");
}
