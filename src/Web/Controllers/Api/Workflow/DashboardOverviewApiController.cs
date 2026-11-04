using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services.Workflow;
namespace Web.Controllers.Api.Workflow;

[Authorize, ApiController, Route("api/workflow-dashboard/overview")]
public class DashboardOverviewApiController(ApprovalDashboardOverviewService overview) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(DateTime? from, DateTime? to, int trendMonths=6, string search=null, CancellationToken ct=default)
        => Ok(await overview.GetAsync(User.Identity.Name,User.IsInRole("SuperUser"),from,to,trendMonths,search,ct));
}
