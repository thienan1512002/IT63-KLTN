using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;

namespace Web.Controllers.Admin;

[Authorize(Roles = RoleConstants.SuperUser)]
[Route("[controller]")]
public class WorkerMonitorController : Controller
{
    [HttpGet]
    [Route("")]
    [Route("[action]")]
    public IActionResult Index() => View();
}
