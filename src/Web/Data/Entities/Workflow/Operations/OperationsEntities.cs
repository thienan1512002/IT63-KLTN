using System.ComponentModel.DataAnnotations;

namespace Web.Data.Entities.Workflow.Operations;

public enum WorkEventKind { BusinessTrip = 1, Leave = 2, VehiclePassenger = 3, Driving = 4, Overtime = 5 }
public enum DispatchStatus { Waiting = 0, Assigned = 1, InProgress = 2, Completed = 3, Cancelled = 4 }
public enum LeaveAllocationStatus { Reserved = 0, Consumed = 1, Released = 2 }

public class EmployeeWorkEvent
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    [Required, MaxLength(450)] public string EmployeeId { get; set; } = string.Empty;
    public WorkEventKind Kind { get; set; }
    [Required, MaxLength(500)] public string Title { get; set; } = string.Empty;
    [MaxLength(1100)] public string Location { get; set; }
    public DateTime StartAt { get; set; }
    // Exclusive end; all-day events end at midnight after the last date.
    public DateTime EndAt { get; set; }
    public bool AllDay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class VehicleDispatch
{
    [Key] public int RequestId { get; set; }
    public int SiteId { get; set; }
    public int? VehicleId { get; set; }
    public bool UseGrab { get; set; }
    [MaxLength(450)] public string DriverEmployeeId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DispatchStatus Status { get; set; }
    public int Revision { get; set; } = 1;
    [MaxLength(1000)] public string Notes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string UpdatedBy { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
}

public class DispatchHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public DispatchStatus PreviousStatus { get; set; }
    public DispatchStatus NewStatus { get; set; }
    public int? VehicleId { get; set; }
    [MaxLength(450)] public string DriverEmployeeId { get; set; }
    [MaxLength(2000)] public string Note { get; set; }
    [MaxLength(256)] public string Actor { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public class DriverProfile
{
    [Key, MaxLength(450)] public string EmployeeId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class FleetVehicle
{
    [Key] public int VehicleId { get; set; }
    public int SiteId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WorkCalendarPolicy
{
    [Key] public int SiteId { get; set; }
    // Bit 0 = Sunday, bit 6 = Saturday. No entitlement is inferred from this policy.
    public int WorkingDaysMask { get; set; }
    public int Revision { get; set; } = 1;
}

public class WorkCalendarException
{
    public int SiteId { get; set; }
    public DateTime Date { get; set; }
    public bool IsWorkingDay { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
}

public class LeaveTypePolicy
{
    public int SiteId { get; set; }
    public int LeaveTypeId { get; set; }
    public bool DeductsBalance { get; set; }
    public int Revision { get; set; } = 1;
}

public class LeaveAccount
{
    [MaxLength(450)] public string EmployeeId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal AnnualDays { get; set; }
    public decimal CarryOverDays { get; set; }
    public int Revision { get; set; } = 1;
    [MaxLength(256)] public string UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class LeaveAdjustment
{
    [Key] public Guid Id { get; set; }
    [MaxLength(450)] public string EmployeeId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal Days { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(256)] public string Actor { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public class LeaveAllocation
{
    public int RequestId { get; set; }
    public int Year { get; set; }
    [Required, MaxLength(450)] public string EmployeeId { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public decimal Days { get; set; }
    public bool PolicyConfigured { get; set; }
    public bool DeductsBalance { get; set; }
    public LeaveAllocationStatus Status { get; set; }
    public decimal? PreviousDays { get; set; }
    public DateTime? ClassifiedAt { get; set; }
    [MaxLength(256)] public string ClassifiedBy { get; set; }
    [MaxLength(500)] public string ClassificationNote { get; set; }
}
