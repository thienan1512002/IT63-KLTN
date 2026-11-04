using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;
using Web.Services.Workflow.Core;
using Worker.Shared.Enums;

namespace Web.Controllers.Workflow
{
    [Route("Workflow")]
    [Authorize]
    public class WorkflowDashboardController : Controller
    {
        private readonly IApprovalNotifier _approvalNotifier;
        public WorkflowDashboardController(IApprovalNotifier approvalNotifier)
        {
            _approvalNotifier = approvalNotifier;
        }
        
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await _approvalNotifier.DismissNotificationsAsync(User.Identity.Name, WorkflowConstants.WorkflowPath.WorkflowListPath); 
            
            return View("~/Views/Workflow/WorkflowDashboard/Index.cshtml");
        }
    }
}
