namespace Web.Models.Dtos.Workflow.Dashboard;

public class DashboardRequestRowDto
{
    public int Id { get; set; }
    public string ApplicationNo { get; set; }
    public string DescriptionNo { get; set; }
    public string RequestName { get; set; }
    public string FormCode { get; set; }
    public DateTime? SubmitDate { get; set; }
    public bool UrgencyLevel { get; set; }
    public string FaxCode { get; set; }
    public string FullName { get; set; }
    public int Process { get; set; }
    public int TotalStep { get; set; }
    public string StatusText { get; set; }
    public List<string> CurrentApprovers { get; set; } = [];
    public string CurrentStepTitle { get; set; }
    /// <summary>Only populated for received-requests: "approved" | "pending" | "waiting"</summary>
    public string MyStepStatus { get; set; }
}
