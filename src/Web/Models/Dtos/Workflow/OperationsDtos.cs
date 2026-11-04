using System.ComponentModel.DataAnnotations;
using Web.Data.Entities.Workflow.Operations;

namespace Web.Models.Dtos.Workflow;

public class DispatchSaveDto
{
    [Range(1, int.MaxValue)] public int Revision { get; set; }
    public int VehicleId { get; set; }
    public bool UseGrab { get; set; }
    [MaxLength(450)] public string DriverEmployeeId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    [MaxLength(1000)] public string Notes { get; set; }
}
public class DispatchActionDto
{
    [Range(1, int.MaxValue)] public int Revision { get; set; }
    public DispatchStatus Status { get; set; }
    [MaxLength(1000)] public string Notes { get; set; }
}
public class LeaveAccountSaveDto
{
    [Required] public string EmployeeId { get; set; }
    [Range(2000, 2200)] public int Year { get; set; }
    public int LeaveTypeId { get; set; }
    [Range(0, 366)] public decimal AnnualDays { get; set; }
    [Range(0, 366)] public decimal CarryOverDays { get; set; }
    public int Revision { get; set; }
    [Required, MaxLength(800)] public string Reason { get; set; }
}
public class LeaveAdjustmentDto
{
    public Guid Id { get; set; }
    [Required] public string EmployeeId { get; set; }
    [Range(2000, 2200)] public int Year { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal Days { get; set; }
    [Required, MaxLength(800)] public string Reason { get; set; }
    public int Revision { get; set; }
}
public class WorkPolicySaveDto
{
    public int SiteId { get; set; }
    [Range(1, 127)] public int WorkingDaysMask { get; set; }
    public int Revision { get; set; }
}
public class CalendarExceptionSaveDto
{
    public int SiteId { get; set; }
    public DateTime Date { get; set; }
    public bool IsWorkingDay { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; }
}
public class LeaveRuleSaveDto
{
    public int SiteId { get; set; }
    public int LeaveTypeId { get; set; }
    public bool DeductsBalance { get; set; }
    public int Revision { get; set; }
}
public class ResourceSaveDto
{
    public string EmployeeId { get; set; }
    public int? VehicleId { get; set; }
    public int SiteId { get; set; }
    public bool IsActive { get; set; } = true;
}
public class LeaveReconcileDto
{
    [Required] public string EmployeeId { get; set; }
    [Range(2000, 2200)] public int Year { get; set; }
    public int LeaveTypeId { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; }
}
public record LeaveBalanceDto(string EmployeeId, int Year, int LeaveTypeId, bool Configured,
    decimal AnnualDays, decimal CarryOverDays, decimal Adjustments, decimal UsedDays, decimal ReservedDays,
    decimal? RemainingDays, decimal? AvailableDays, int Revision, bool PolicyConfigured, bool DeductsBalance, int UnclassifiedRequests);
public record LeaveYearQuoteDto(int Year, decimal Days, LeaveBalanceDto Balance);
