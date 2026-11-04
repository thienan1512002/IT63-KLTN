using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers.Workflow;

[Authorize]
public class OperationsController : Controller
{
    [HttpGet("Operations")]
    public IActionResult Index() => View("~/Views/Workflow/Operations/Index.cshtml");
}
