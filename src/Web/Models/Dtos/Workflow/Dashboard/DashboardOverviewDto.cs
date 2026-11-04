using Web.Data.Enums.Workflow;
namespace Web.Models.Dtos.Workflow.Dashboard;

public record DashboardSnapshot(int Total, int Completed, int Pending, int Returned, double? AverageHours, int TimedCompletions)
{
    public double? CompletionRate => Total == 0 ? null : Math.Round(Completed * 100d / Total, 1);
}
public record DashboardTrend(int Year, int Month, int Total, int Completed, int Pending);
public record DashboardStatusCount(RequestStatus Status, int Count);
public record DashboardCategory(string Name, string TemplateCode, int Count);
public record DashboardDepartment(string Name, int Count, int Completed);
public record DashboardApprover(string Name, string Position, int Count);
public record DashboardWorkflow(string Name, List<string> RequestTypes, int StepCount, bool IsActive);
public record DashboardAttention(int Id, string ApplicationNo, string RequestType, string CreatedBy, RequestStatus Status, RequestPriority Priority, DateTime SentAt);
public record DashboardActivity(int RequestId, string ApplicationNo, string Actor, ApprovalAction Action, DateTime ActionAt);
public class DashboardOverviewDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DashboardSnapshot Current { get; set; }
    public DashboardSnapshot Previous { get; set; }
    // These measures stay null until the corresponding audit data / SLA policy exists.
    public int? AutomaticApprovals { get; set; }
    public double? AutomationRate { get; set; }
    public double? SlaRate { get; set; }
    public int ApprovalActions { get; set; }
    public int ActiveWorkflows { get; set; }
    public List<DashboardTrend> Trend { get; set; } = [];
    public List<DashboardStatusCount> Statuses { get; set; } = [];
    public List<DashboardCategory> Categories { get; set; } = [];
    public List<DashboardDepartment> Departments { get; set; } = [];
    public List<DashboardApprover> Approvers { get; set; } = [];
    public List<DashboardWorkflow> Workflows { get; set; } = [];
    public List<DashboardAttention> Attention { get; set; } = [];
    public List<DashboardActivity> Activities { get; set; } = [];
}
