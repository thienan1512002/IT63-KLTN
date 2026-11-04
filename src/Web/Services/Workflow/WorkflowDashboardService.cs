using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow.Dashboard;

namespace Web.Services.Workflow;

public class WorkflowDashboardService : IWorkflowDashboardService
{
    private readonly ApplicationDbContext _context;

    public WorkflowDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetSentStatsAsync(string userId)
    {
        var query = _context.ApprovalRequests
            .Where(r => r.FaxCode == userId && !r.IsRemoved);

        var total = await query.CountAsync();
        var ongoing = await query.CountAsync(r => r.Status == RequestStatus.Ongoing);
        var completed = await query.CountAsync(r => r.Status == RequestStatus.Completed);
        var rejected = await query.CountAsync(r => r.Status == RequestStatus.Rejected);

        return new { total, ongoing, completed, rejected };
    }

    public Task<IQueryable<DashboardRequestRowDto>> BuildSentRequestsQueryAsync(string userId, string status)
    {
        var query = _context.ApprovalRequests
            .Where(r => r.FaxCode == userId && !r.IsRemoved);

        if (status == "ongoing") query = query.Where(r => r.Status == RequestStatus.Ongoing);
        else if (status == "completed") query = query.Where(r => r.Status == RequestStatus.Completed);
        else if (status == "rejected") query = query.Where(r => r.Status == RequestStatus.Rejected);

        var rows = query
            .OrderByDescending(r => r.SubmitDate)
            .Select(r => new DashboardRequestRowDto
        {
            Id = r.Id,
            ApplicationNo = r.ApplicationNo,
            DescriptionNo = r.DescriptionNo,
            RequestName = r.RequestName,
            FormCode = r.FormCode,
            SubmitDate = r.SubmitDate,
            UrgencyLevel = r.UrgencyLevel,
            FaxCode = r.FaxCode,
            FullName = r.FullName,
            Process = r.Process,
            TotalStep = r.TotalStep,
            StatusText = r.Status == RequestStatus.Ongoing ? "ongoing"
                       : r.Status == RequestStatus.Completed ? "completed"
                       : r.Status == RequestStatus.Rejected ? "rejected"
                       : r.Status == RequestStatus.Returned ? "returned"
                       : "unknown",
            CurrentApprovers = r.ApprovalDetails
                .Where(d => d.IsCurrentStep)
                .SelectMany(d => d.ApproverInfos)
                .OrderBy(a => a.Id)
                .Select(a => a.UserName ?? a.FullName)
                .ToList(),
            CurrentStepTitle = r.ApprovalDetails
                .Where(d => d.IsCurrentStep)
                .Select(d => d.StepTitle)
                .FirstOrDefault(),
        });

        return Task.FromResult(rows);
    }

    public async Task<object> GetReceivedStatsAsync(string userId)
    {
        var requestIds = await _context.ApproverInfos
            .Where(a => a.UserName == userId && !a.ApprovalDetail.IsRequester)
            .Select(a => a.ApprovalDetail.RequestId)
            .Distinct()
            .ToListAsync();

        var query = _context.ApprovalRequests
            .Where(r => requestIds.Contains(r.Id) && !r.IsRemoved);

        var pendingIds = await _context.ApprovalDetails
            .Where(d => requestIds.Contains(d.RequestId)
                && d.IsCurrentStep && !d.IsApproved && !d.IsRequester
                && d.ApproverInfos.Any(a => a.UserName == userId))
            .Select(d => d.RequestId)
            .Distinct()
            .ToListAsync();

        var approvedIds = await _context.ApprovalDetails
            .Where(d => requestIds.Contains(d.RequestId)
                && d.IsApproved && d.ApprovedBy == userId && !d.IsRequester)
            .Select(d => d.RequestId)
            .Distinct()
            .ToListAsync();

        var pending = await query.CountAsync(r => pendingIds.Contains(r.Id));
        var approvedByMe = await query.CountAsync(r => approvedIds.Contains(r.Id));

        var completed = await query.CountAsync(r => r.Status == RequestStatus.Completed);
        var rejected = await query.CountAsync(r => r.Status == RequestStatus.Rejected);

        return new { approvedByMe, pending, completed, rejected };
    }

    public async Task<IQueryable<DashboardRequestRowDto>> BuildReceivedRequestsQueryAsync(string userId, string status)
    {
        var requestIds = await _context.ApproverInfos
            .Where(a => a.UserName == userId && !a.ApprovalDetail.IsRequester)
            .Select(a => a.ApprovalDetail.RequestId)
            .Distinct()
            .ToListAsync();

        var query = _context.ApprovalRequests
            .Where(r => requestIds.Contains(r.Id) && !r.IsRemoved);

        if (status == "pending")
        {
            var pendingIds = await _context.ApprovalDetails
                .Where(d => requestIds.Contains(d.RequestId)
                    && d.IsCurrentStep && !d.IsApproved && !d.IsRequester
                    && d.ApproverInfos.Any(a => a.UserName == userId))
                .Select(d => d.RequestId)
                .Distinct()
                .ToListAsync();
            query = query.Where(r => pendingIds.Contains(r.Id));
        }
        else if (status == "approvedByMe")
        {
            var approvedIds = await _context.ApprovalDetails
                .Where(d => requestIds.Contains(d.RequestId)
                    && d.IsApproved && d.ApprovedBy == userId && !d.IsRequester)
                .Select(d => d.RequestId)
                .Distinct()
                .ToListAsync();
            query = query.Where(r => approvedIds.Contains(r.Id));
        }
        else if (status == "completed") query = query.Where(r => r.Status == RequestStatus.Completed);
        else if (status == "rejected") query = query.Where(r => r.Status == RequestStatus.Rejected);

        var rows = query
            .OrderByDescending(r => r.SubmitDate)
            .Select(r => new DashboardRequestRowDto
        {
            Id = r.Id,
            ApplicationNo = r.ApplicationNo,
            DescriptionNo = r.DescriptionNo,
            RequestName = r.RequestName,
            FormCode = r.FormCode,
            SubmitDate = r.SubmitDate,
            UrgencyLevel = r.UrgencyLevel,
            FaxCode = r.FaxCode,
            FullName = r.FullName,
            Process = r.Process,
            TotalStep = r.TotalStep,
            StatusText = r.Status == RequestStatus.Ongoing ? "ongoing"
                       : r.Status == RequestStatus.Completed ? "completed"
                       : r.Status == RequestStatus.Rejected ? "rejected"
                       : r.Status == RequestStatus.Returned ? "returned"
                       : "unknown",
            CurrentApprovers = r.ApprovalDetails
                .Where(d => d.IsCurrentStep)
                .SelectMany(d => d.ApproverInfos)
                .OrderBy(a => a.Id)
                .Select(a => a.UserName ?? a.FullName)
                .ToList(),
            CurrentStepTitle = r.ApprovalDetails
                .Where(d => d.IsCurrentStep)
                .Select(d => d.StepTitle)
                .FirstOrDefault(),
            MyStepStatus = r.ApprovalDetails
                .Where(d => d.ApproverInfos.Any(a => a.UserName == userId))
                .OrderByDescending(d => d.IsCurrentStep)
                .ThenByDescending(d => d.StepOrder)
                .Select(d => d.IsApproved ? "approved" : d.IsCurrentStep ? "pending" : "waiting")
                .FirstOrDefault() ?? "waiting",
        });

        return rows;
    }

    public async Task<(int Pending, int Ongoing)> GetHomeStatsAsync(string userId)
    {
        // pending = all requests currently waiting for THIS user to approve
        var requestIds = await _context.ApproverInfos
            .Where(a => a.UserName == userId && !a.ApprovalDetail.IsRequester)
            .Select(a => a.ApprovalDetail.RequestId)
            .Distinct()
            .ToListAsync();

        var pending = await _context.ApprovalDetails
            .Where(d => requestIds.Contains(d.RequestId)
                && d.IsCurrentStep && !d.IsApproved && !d.IsRequester
                && d.ApproverInfos.Any(a => a.UserName == userId))
            .Select(d => d.RequestId)
            .Distinct()
            .CountAsync();

        // ongoing = all requests submitted by user that are still in progress
        var ongoing = await _context.ApprovalRequests
            .CountAsync(r => r.FaxCode == userId && !r.IsRemoved
                && r.Status == RequestStatus.Ongoing);

        return (pending, ongoing);
    }
}
