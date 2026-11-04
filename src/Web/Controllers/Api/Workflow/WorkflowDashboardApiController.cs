using Microsoft.AspNetCore.Authorization;
using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Mvc;
using Web.Common;
using Web.Services.Workflow;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;

namespace Web.Controllers.Api.Workflow
{
    [ApiController]
    [Route("api/workflow-dashboard")]
    [Authorize]
    public class WorkflowDashboardApiController : ControllerBase
    {
        private readonly IWorkflowDashboardService _dashboardService;
        private readonly ApplicationDbContext _db;

        public WorkflowDashboardApiController(IWorkflowDashboardService dashboardService, ApplicationDbContext db)
        {
            _dashboardService = dashboardService;
            _db = db;
        }

        private IQueryable<ApprovalRequest> VisibleRequests()
        {
            var query = _db.ApprovalRequests.AsNoTracking();
            if (!User.IsInRole(RoleConstants.SuperUser))
                query = query.Where(x => x.FaxCode == User.Identity.Name ||
                    x.ApprovalDetails.Any(d => d.ApproverInfos.Any(a => a.UserName == User.Identity.Name)));
            return query;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary(DateTime? from, DateTime? to, CancellationToken ct)
        {
            var query = VisibleRequests();
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from);
            if (to.HasValue) { var end = to.Value.Date.AddDays(1); query = query.Where(x => x.CreatedAt < end); }
            var statuses = await query.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
            return Ok(new {
                Total = statuses.Sum(x => x.Count),
                Pending = statuses.Where(x => x.Status == RequestStatus.Ongoing).Sum(x => x.Count),
                Completed = statuses.Where(x => x.Status == RequestStatus.Completed).Sum(x => x.Count),
                Returned = statuses.Where(x => x.Status == RequestStatus.Returned).Sum(x => x.Count),
                Rejected = statuses.Where(x => x.Status == RequestStatus.Rejected).Sum(x => x.Count),
                ByStatus = statuses,
                ByType = await query.GroupBy(x => x.RequestType.Name).Select(g => new { Name = g.Key, Count = g.Count() }).ToListAsync(ct),
                ByDepartment = await query.GroupBy(x => x.Department.Name).Select(g => new { Name = g.Key, Count = g.Count() }).ToListAsync(ct)
            });
        }

        [HttpGet("report")]
        public async Task<IActionResult> Report(DataSourceLoadOptions options, DateTime? from, DateTime? to, CancellationToken ct)
        {
            var query = VisibleRequests();
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from);
            if (to.HasValue) { var end = to.Value.Date.AddDays(1); query = query.Where(x => x.CreatedAt < end); }
            return Ok(await DataSourceLoader.LoadAsync(query.Select(x => new {
                x.Id, x.ApplicationNo, x.Title, RequestType = x.RequestType.Name, CreatedBy = x.FullName,
                Department = x.Department.Name, x.Status, x.CreatedAt, x.SubmittedAt
            }), options, ct));
        }

        // ── Sent Requests ─────────────────────────────────────────────────────

        [HttpGet("sent-stats")]
        public async Task<IActionResult> GetSentStats()
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            return Ok(await _dashboardService.GetSentStatsAsync(userId));
        }

        [HttpGet("sent-requests")]
        public async Task<IActionResult> GetSentRequests(
            [FromQuery] DataSourceLoadOptions loadOptions,
            [FromQuery] string status = "all",
            CancellationToken cancellationToken = default)
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = await _dashboardService.BuildSentRequestsQueryAsync(userId, status);
            var result = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
            return Ok(new { result.totalCount, result.data });
        }

        // ── Received Requests ─────────────────────────────────────────────────

        [HttpGet("received-stats")]
        public async Task<IActionResult> GetReceivedStats()
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            return Ok(await _dashboardService.GetReceivedStatsAsync(userId));
        }

        [HttpGet("received-requests")]
        public async Task<IActionResult> GetReceivedRequests(
            [FromQuery] DataSourceLoadOptions loadOptions,
            [FromQuery] string status = "all",
            CancellationToken cancellationToken = default)
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = await _dashboardService.BuildReceivedRequestsQueryAsync(userId, status);
            var result = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
            return Ok(new { result.totalCount, result.data });
        }
    }
}
