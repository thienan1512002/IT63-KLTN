using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow.Dashboard;

namespace Web.Services.Workflow;

public class ApprovalDashboardOverviewService(ApplicationDbContext db)
{
    private static readonly TimeSpan DisplayOffset = TimeSpan.FromHours(7);
    private IQueryable<ApprovalRequest> Visible(string userName, bool admin) => db.ApprovalRequests.AsNoTracking()
        .Where(x => !x.IsRemoved && (admin || x.FaxCode == userName || x.ApprovalDetails.Any(d => d.ApproverInfos.Any(a => a.UserName == userName))));
    private static async Task<DashboardSnapshot> Snapshot(IQueryable<ApprovalRequest> query, CancellationToken ct)
    {
        return await query.GroupBy(x => 1).Select(g => new DashboardSnapshot(
            g.Count(), g.Count(x => x.Status == RequestStatus.Completed), g.Count(x => x.Status == RequestStatus.Ongoing), g.Count(x => x.Status == RequestStatus.Returned),
            g.Average(x => x.Status == RequestStatus.Completed && x.SubmittedAt.HasValue && x.CompletedAt.HasValue && x.CompletedAt >= x.SubmittedAt
                ? (double?)EF.Functions.DateDiffMinute(x.SubmittedAt.Value, x.CompletedAt.Value) / 60 : null),
            g.Count(x => x.Status == RequestStatus.Completed && x.SubmittedAt.HasValue && x.CompletedAt.HasValue && x.CompletedAt >= x.SubmittedAt)
        )).SingleOrDefaultAsync(ct) ?? new(0, 0, 0, 0, null, 0);
    }
    public async Task<DashboardOverviewDto> GetAsync(string userName, bool admin, DateTime? from, DateTime? to, int trendMonths, string search, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Add(DisplayOffset).Date;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var finish = (to ?? today).Date;
        if (start > finish || finish >= DateTime.MaxValue.Date || (finish - start).TotalDays > 3660 || trendMonths is < 1 or > 12)
            throw new ArgumentException("Chọn khoảng ngày hợp lệ (tối đa 10 năm) và xu hướng từ 1 đến 12 tháng.");
        if (search?.Length > 200) throw new ArgumentException("Nội dung tìm kiếm không quá 200 ký tự.");
        var lower = DateTime.SpecifyKind(start - DisplayOffset, DateTimeKind.Utc);
        var upper = DateTime.SpecifyKind(finish.AddDays(1) - DisplayOffset, DateTimeKind.Utc);
        var previousStart = lower.AddDays(-(finish - start).Days - 1);
        var visible = Visible(userName, admin);
        var query = visible;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.ApplicationNo.Contains(term) || x.Title.Contains(term) || x.FullName.Contains(term));
        }
        var cohort = query.Where(x => x.CreatedAt >= lower && x.CreatedAt < upper);
        var result = new DashboardOverviewDto { From=start, To=finish, GeneratedAt=DateTime.UtcNow };
        result.Current = await Snapshot(cohort, ct);
        result.Previous = await Snapshot(query.Where(x => x.CreatedAt >= previousStart && x.CreatedAt < lower), ct);
        result.Statuses = await cohort.GroupBy(x => x.Status).Select(g => new DashboardStatusCount(g.Key, g.Count())).ToListAsync(ct);
        result.Categories = await cohort.GroupBy(x => new { x.RequestType.Name, x.RequestType.TemplateCode }).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Name).Select(g => new DashboardCategory(g.Key.Name, g.Key.TemplateCode, g.Count())).ToListAsync(ct);
        result.Departments = await cohort.GroupBy(x => x.Department.Name).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => new DashboardDepartment(g.Key, g.Count(), g.Count(x => x.Status == RequestStatus.Completed))).ToListAsync(ct);
        var trendStart = new DateTime(finish.Year, finish.Month, 1).AddMonths(1 - trendMonths);
        var trendLower = DateTime.SpecifyKind(trendStart - DisplayOffset, DateTimeKind.Utc);
        var points = await query.Where(x => x.CreatedAt >= trendLower && x.CreatedAt < upper)
            .GroupBy(x => new { Year=x.CreatedAt.AddHours(7).Year, Month=x.CreatedAt.AddHours(7).Month })
            .Select(g => new DashboardTrend(g.Key.Year, g.Key.Month, g.Count(), g.Count(x=>x.Status==RequestStatus.Completed), g.Count(x=>x.Status==RequestStatus.Ongoing))).ToListAsync(ct);
        result.Trend = Enumerable.Range(0,trendMonths).Select(index => { var month=trendStart.AddMonths(index); return points.FirstOrDefault(x=>x.Year==month.Year && x.Month==month.Month) ?? new(month.Year,month.Month,0,0,0); }).ToList();
        var actions = db.ApprovalHistories.AsNoTracking().Where(h => cohort.Any(r => r.Id==h.RequestId) && h.ActionAt>=lower && h.ActionAt<upper);
        result.ApprovalActions = await actions.CountAsync(x=>x.Action==ApprovalAction.Approved, ct);
        result.Approvers = await actions.Where(x=>x.Action==ApprovalAction.Approved).GroupBy(x=>x.ActionByUserId)
            .Select(g=>new { UserName=g.Key, Count=g.Count() }).OrderByDescending(x=>x.Count).ThenBy(x=>x.UserName).Take(5)
            .Select(x=>new DashboardApprover(db.AppUsers.Where(u=>u.UserName==x.UserName).Select(u=>u.FullName).FirstOrDefault() ?? x.UserName,
                db.AppUsers.Where(u=>u.UserName==x.UserName).Select(u=>u.Position).FirstOrDefault(),x.Count)).ToListAsync(ct);
        result.Attention = await cohort.Where(x=>x.Status==RequestStatus.Ongoing || x.Status==RequestStatus.Returned)
            .OrderByDescending(x=>x.Priority).ThenBy(x=>x.SubmittedAt ?? x.CreatedAt).ThenBy(x=>x.Id).Take(5)
            .Select(x=>new DashboardAttention(x.Id,x.ApplicationNo,x.RequestType.Name,x.FullName,x.Status,x.Priority,x.SubmittedAt ?? x.CreatedAt)).ToListAsync(ct);
        result.Activities = await actions.OrderByDescending(x=>x.ActionAt).ThenByDescending(x=>x.Id).Take(5)
            .Select(x=>new DashboardActivity(x.RequestId,x.Request.ApplicationNo,db.AppUsers.Where(u=>u.UserName==x.ActionByUserId).Select(u=>u.FullName).FirstOrDefault() ?? x.ActionByUserId,x.Action,x.ActionAt)).ToListAsync(ct);
        var workflows = db.ApprovalCases.AsNoTracking().Where(c=>admin || visible.Any(r=>r.ApprovalCaseId==c.CaseId));
        result.ActiveWorkflows = await workflows.CountAsync(x=>x.IsActive,ct);
        result.Workflows = await workflows.OrderByDescending(x=>x.IsActive).ThenBy(x=>x.CaseName).Take(5)
            .Select(c=>new DashboardWorkflow(c.CaseName,db.RequestTypes.Where(t=>t.ApprovalCaseId==c.CaseId && t.IsActive).OrderBy(t=>t.Name).Select(t=>t.Name).ToList(),db.ApprovalSteps.Count(s=>s.ApprovalCaseId==c.CaseId && !s.IsRemoved),c.IsActive)).ToListAsync(ct);
        return result;
    }
}

