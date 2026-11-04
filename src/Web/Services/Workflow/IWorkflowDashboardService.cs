using Web.Models.Dtos.Workflow.Dashboard;

namespace Web.Services.Workflow;

public interface IWorkflowDashboardService
{
    Task<object> GetSentStatsAsync(string userId);
    Task<IQueryable<DashboardRequestRowDto>> BuildSentRequestsQueryAsync(string userId, string status);
    Task<object> GetReceivedStatsAsync(string userId);
    Task<IQueryable<DashboardRequestRowDto>> BuildReceivedRequestsQueryAsync(string userId, string status);
    Task<(int Pending, int Ongoing)> GetHomeStatsAsync(string userId);
}
