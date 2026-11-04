using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow.Operations;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Workflow.Operations;

namespace Web.Controllers.Api.Workflow;

public class OperationsExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch { UnauthorizedAccessException => 403, KeyNotFoundException => 404,
            DbUpdateConcurrencyException => 409, InvalidOperationException or ArgumentException => 400,
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 or 1205 } } => 409,
            SqlException { Number: 1205 } => 409, _ => 0 };
        if (status == 0) return;
        var message = context.Exception is SqlException or DbUpdateException and not DbUpdateConcurrencyException
            ? "Dữ liệu vừa thay đổi bởi thao tác khác. Hãy tải lại trước khi tiếp tục." : context.Exception.Message;
        context.Result = new ObjectResult(new { message }) { StatusCode = status }; context.ExceptionHandled = true;
    }
}

[Authorize, ApiController, Route("api/operations"), TypeFilter(typeof(OperationsExceptionFilter))]
public class OperationsApiController(ApplicationDbContext db, LeaveAccountingService leave,
    ApprovalOperationsService operations, VehicleDispatchService dispatch) : ControllerBase
{
    private Task<OperationsActor> Actor(CancellationToken ct) => OperationsActor.ResolveAsync(db, User, ct);
    private static (DateTime From, DateTime To) Range(DateTime from, DateTime to)
    {
        if (from.Year < 2000 || to.Year > 2200 || to.Date < from.Date || (to.Date - from.Date).Days > 366)
            throw new InvalidOperationException("Khoảng ngày phải hợp lệ và không dài hơn một năm.");
        return (from.Date, to.Date.AddDays(1));
    }
    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken ct)
    {
        var a = await Actor(ct);
        return Ok(new {
            CurrentEmployeeId = a.UserId, a.SiteId, a.HRRead, a.HRWrite, CanDispatch = a.Dispatch, a.IsAdmin,
            Employees = await db.Users.AsNoTracking().Where(x => x.IsActive && (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow) &&
                (a.IsAdmin || x.SiteId == a.SiteId && a.SiteId.HasValue) && (a.HRRead || a.Dispatch || x.Id == a.UserId))
                .OrderBy(x => x.FullName).Select(x => new { x.Id, Name = x.FullName, x.UserName, x.SiteId,
                    IsDriver = db.DriverProfiles.Any(d => d.EmployeeId == x.Id && d.IsActive) }).ToListAsync(ct),
            Sites = await db.BelongSites.Where(x => x.IsActive && (a.IsAdmin || x.Id == a.SiteId)).Select(x => new { x.Id, x.Name }).ToListAsync(ct),
            LeaveTypes = await db.RequestMasterItems.Where(x => x.Category == "LeaveType").Select(x => new { x.Id, x.Name, x.IsActive }).ToListAsync(ct),
            Vehicles = await (from f in db.FleetVehicles join v in db.RequestMasterItems on f.VehicleId equals v.Id
                where a.IsAdmin || f.SiteId == a.SiteId && a.SiteId.HasValue
                select new { Id = v.Id, v.Name, v.Code, f.SiteId, v.ParentId, v.Capacity, IsActive = f.IsActive && v.IsActive }).ToListAsync(ct),
            VehicleCatalog = a.Dispatch ? await db.RequestMasterItems.Where(x => x.Category == "Vehicle" && x.IsActive).Select(x => new { x.Id, x.Name, x.Code }).ToListAsync(ct) : null,
            Drivers = await (from p in db.DriverProfiles join u in db.Users on p.EmployeeId equals u.Id
                where (a.IsAdmin || u.SiteId == a.SiteId && a.SiteId.HasValue) && (a.Dispatch || a.HRRead || u.Id == a.UserId)
                select new { u.Id, Name = u.FullName, u.SiteId, IsActive = p.IsActive && u.IsActive && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow) }).ToListAsync(ct)
        });
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar(DateTime from, DateTime to, string employeeId, WorkEventKind? kind, CancellationToken ct)
    {
        var a = await Actor(ct); var range = Range(from, to);
        if (kind.HasValue && !Enum.IsDefined(kind.Value)) throw new InvalidOperationException("Loại lịch không hợp lệ.");
        if (!string.IsNullOrEmpty(employeeId)) await a.RequireEmployeeAsync(db, employeeId, true, true, ct);
        var allowed = db.Users.Where(u => u.Id == a.UserId || a.IsAdmin || a.SiteId.HasValue && u.SiteId == a.SiteId &&
            (a.HRRead || a.Dispatch && db.DriverProfiles.Any(d => d.EmployeeId == u.Id))).Select(u => u.Id);
        var rows = await (from e in db.EmployeeWorkEvents.AsNoTracking() join u in db.Users on e.EmployeeId equals u.Id
            join r in db.ApprovalRequests on e.RequestId equals r.Id
            where allowed.Contains(e.EmployeeId) && e.StartAt < range.To && e.EndAt > range.From &&
                (employeeId == null || e.EmployeeId == employeeId) && (!kind.HasValue || e.Kind == kind) && !r.IsRemoved
            orderby e.StartAt, u.FullName
            select new { e.Id, e.EmployeeId, Employee = u.FullName, e.Kind,
                Title = a.HRRead || e.EmployeeId == a.UserId || e.Kind == WorkEventKind.Driving ? e.Title : "Không khả dụng",
                Location = a.HRRead || e.EmployeeId == a.UserId || e.Kind == WorkEventKind.Driving ? e.Location : "",
                e.StartAt, e.EndAt, e.AllDay,
                RequestId = a.HRRead || e.EmployeeId == a.UserId || e.Kind == WorkEventKind.Driving ? (int?)r.Id : null,
                ApplicationNo = a.HRRead || e.EmployeeId == a.UserId || e.Kind == WorkEventKind.Driving ? r.ApplicationNo : "",
                CanViewRequest = a.IsAdmin || r.FaxCode == a.UserName || r.BusinessData.EmployeeId == a.UserId || r.BusinessData.Participants.Any(p => p.EmployeeId == a.UserId) ||
                    r.ApprovalDetails.Any(d => d.ApproverInfos.Any(p => p.UserName == a.UserName)) || db.VehicleDispatches.Any(d => d.RequestId == r.Id && d.DriverEmployeeId == a.UserId)
            }).Take(5001).ToListAsync(ct);
        return Ok(new { Events = rows.Take(5000), Truncated = rows.Count > 5000 });
    }

    [HttpGet("dispatches")]
    public async Task<IActionResult> Dispatches(DateTime from, DateTime to, DispatchStatus? status, int? requestId, string driverId, int? vehicleId, CancellationToken ct)
    {
        var a = await Actor(ct); var range = Range(from, to);
        if (status.HasValue && !Enum.IsDefined(status.Value)) throw new InvalidOperationException("Trạng thái chuyến xe không hợp lệ.");
        var rows = await (from d in db.VehicleDispatches.AsNoTracking() join r in db.ApprovalRequests on d.RequestId equals r.Id
            where (a.IsAdmin || a.Dispatch && a.SiteId.HasValue && d.SiteId == a.SiteId || r.FaxCode == a.UserName || d.DriverEmployeeId == a.UserId || r.BusinessData.EmployeeId == a.UserId || r.BusinessData.Participants.Any(p => p.EmployeeId == a.UserId)) &&
                (requestId.HasValue ? d.RequestId == requestId : d.StartAt < range.To && d.EndAt > range.From) &&
                (!status.HasValue || d.Status == status) && (driverId == null || d.DriverEmployeeId == driverId) && (!vehicleId.HasValue || d.VehicleId == vehicleId) && !r.IsRemoved
            orderby d.StartAt
            select new { d.RequestId, r.ApplicationNo, r.Title, d.SiteId, d.Status, d.Revision, d.StartAt, d.EndAt,
                d.VehicleId, d.UseGrab, Vehicle = db.RequestMasterItems.Where(x => x.Id == d.VehicleId).Select(x => x.Name).FirstOrDefault(),
                d.DriverEmployeeId, Driver = db.Users.Where(x => x.Id == d.DriverEmployeeId).Select(x => x.FullName).FirstOrDefault(),
                r.BusinessData.VehicleTypeId, r.BusinessData.PassengerCount, r.BusinessData.PickupLocation, r.BusinessData.Destination, r.BusinessData.ContactPhone,
                SuggestedVehicleId = r.BusinessData.VehicleId, SuggestedDriverId = r.BusinessData.DriverEmployeeId,
                Contact = db.Users.Where(x => x.Id == r.BusinessData.EmployeeId).Select(x => x.FullName).FirstOrDefault(),
                d.Notes, d.ActualStartAt, d.ActualEndAt, CanAssign = a.Dispatch && (a.IsAdmin || a.SiteId == d.SiteId),
                CanDrive = d.DriverEmployeeId == a.UserId, CanViewRequest = a.IsAdmin || r.FaxCode == a.UserName || r.BusinessData.EmployeeId == a.UserId ||
                    r.BusinessData.Participants.Any(p => p.EmployeeId == a.UserId) || r.ApprovalDetails.Any(s => s.ApproverInfos.Any(p => p.UserName == a.UserName)) || d.DriverEmployeeId == a.UserId
            }).Take(2001).ToListAsync(ct);
        return Ok(new { Items = rows.Take(2000), Truncated = rows.Count > 2000 });
    }
    [HttpPut("dispatches/{id:int}")]
    public async Task<IActionResult> Assign(int id, DispatchSaveDto input, CancellationToken ct) { await dispatch.AssignAsync(id, input, await Actor(ct), ct); return NoContent(); }
    [HttpPost("dispatches/{id:int}/status")]
    public async Task<IActionResult> DispatchAction(int id, DispatchActionDto input, CancellationToken ct) { await dispatch.TransitionAsync(id, input, await Actor(ct), ct); return NoContent(); }
    [HttpGet("dispatches/{id:int}/history")]
    public async Task<IActionResult> DispatchHistory(int id, CancellationToken ct)
    {
        var a = await Actor(ct); var job = await db.VehicleDispatches.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == id, ct) ?? throw new KeyNotFoundException();
        if (!a.IsAdmin && !(a.Dispatch && a.SiteId == job.SiteId) && job.DriverEmployeeId != a.UserId &&
            !await db.ApprovalRequests.AnyAsync(x => x.Id == id && (x.FaxCode == a.UserName || x.BusinessData.EmployeeId == a.UserId || x.BusinessData.Participants.Any(p => p.EmployeeId == a.UserId)), ct)) throw new UnauthorizedAccessException();
        return Ok(await db.DispatchHistories.AsNoTracking().Where(x => x.RequestId == id).OrderByDescending(x => x.At).Select(x => new { x.At, x.Actor, x.PreviousStatus, x.NewStatus, x.Note }).ToListAsync(ct));
    }

    [HttpGet("leave/balances")]
    public async Task<IActionResult> Balances(string employeeId, int year, CancellationToken ct)
    {
        var a = await Actor(ct); employeeId ??= a.UserId; await a.RequireEmployeeAsync(db, employeeId, true, false, ct);
        var types = await db.RequestMasterItems.Where(x => x.Category == "LeaveType").OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var rows = new List<object>();
        foreach (var type in types) rows.Add(new { LeaveType = type.Name, Balance = await leave.BalanceAsync(employeeId, year, type.Id, null, ct) });
        return Ok(rows);
    }
    [HttpGet("leave/quote")]
    public async Task<IActionResult> Quote(string employeeId, int leaveTypeId, DateTime startAt, DateTime endAt, string startSession, string endSession, int? requestId, CancellationToken ct)
    {
        var a = await Actor(ct);
        // Request creators may select employees in their own site, as allowed by the form.
        var targetSite = await db.Users.Where(x => x.Id == employeeId).Select(x => x.SiteId).SingleOrDefaultAsync(ct);
        var canSeeBalance = employeeId == a.UserId || a.HRRead && (a.IsAdmin || a.SiteId == targetSite && a.SiteId.HasValue);
        if (!canSeeBalance) throw new UnauthorizedAccessException("Chỉ nhân viên hoặc HR được xem số dư phép cá nhân.");
        if (requestId.HasValue && !await db.ApprovalRequests.AnyAsync(x => x.Id == requestId && (a.IsAdmin || x.FaxCode == a.UserName || a.HRRead && x.SiteId == a.SiteId), ct)) throw new UnauthorizedAccessException();
        var quote = await leave.QuoteAsync(employeeId, leaveTypeId, startAt, endAt, startSession, endSession, requestId, ct);
        return Ok(new { Years = quote, WorkingCalendarConfigured = await db.WorkCalendarPolicies.AnyAsync(x => x.SiteId == targetSite, ct) });
    }
    [HttpGet("leave/history")]
    public async Task<IActionResult> LeaveHistory(string employeeId, int year, int leaveTypeId, CancellationToken ct)
    {
        var a = await Actor(ct); employeeId ??= a.UserId; await a.RequireEmployeeAsync(db, employeeId, true, false, ct);
        return Ok(new {
            Adjustments = await db.LeaveAdjustments.AsNoTracking().Where(x => x.EmployeeId == employeeId && x.Year == year && x.LeaveTypeId == leaveTypeId).OrderByDescending(x => x.At).Select(x => new { x.Id, x.At, x.Actor, x.Days, x.Reason }).ToListAsync(ct),
            Requests = await (from l in db.LeaveAllocations.AsNoTracking() join r in db.ApprovalRequests on l.RequestId equals r.Id
                where l.EmployeeId == employeeId && l.Year == year && l.LeaveTypeId == leaveTypeId
                orderby r.CreatedAt descending select new { l.RequestId, r.ApplicationNo, r.Title, l.Days, l.Status, l.PolicyConfigured, l.DeductsBalance, l.PreviousDays, l.ClassifiedAt, l.ClassifiedBy, l.ClassificationNote,
                    r.BusinessData.StartAt, r.BusinessData.EndAt, CanViewRequest = a.IsAdmin || r.FaxCode == a.UserName || r.BusinessData.EmployeeId == a.UserId || r.ApprovalDetails.Any(s => s.ApproverInfos.Any(p => p.UserName == a.UserName)) }).ToListAsync(ct)
        });
    }
    [HttpPut("leave/accounts")]
    public async Task<IActionResult> SaveAccount(LeaveAccountSaveDto input, CancellationToken ct) { await leave.SaveAccountAsync(input, await Actor(ct), ct); return NoContent(); }
    [HttpPost("leave/adjustments")]
    public async Task<IActionResult> Adjust(LeaveAdjustmentDto input, CancellationToken ct) { await leave.AdjustAsync(input, await Actor(ct), ct); return NoContent(); }
    [HttpPost("leave/reconcile")]
    public async Task<IActionResult> Reconcile(LeaveReconcileDto input, CancellationToken ct) => Ok(new { Updated = await leave.ReconcileAsync(input, await Actor(ct), ct) });
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct) => Ok(await operations.BackfillAsync(await Actor(ct), ct));

    [HttpGet("settings")]
    public async Task<IActionResult> Settings(int siteId, int year, CancellationToken ct)
    {
        var a = await Actor(ct); if (!a.HRRead) throw new UnauthorizedAccessException(); a.RequireSite(siteId);
        if (year is < 2000 or > 2200) throw new InvalidOperationException("Năm không hợp lệ.");
        return Ok(new {
            Calendar = await db.WorkCalendarPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == siteId, ct),
            Exceptions = await db.WorkCalendarExceptions.AsNoTracking().Where(x => x.SiteId == siteId && x.Date.Year == year).OrderBy(x => x.Date).ToListAsync(ct),
            LeaveRules = await db.LeaveTypePolicies.AsNoTracking().Where(x => x.SiteId == siteId).ToListAsync(ct)
        });
    }
    private async Task<OperationsActor> SettingsActor(int siteId, CancellationToken ct)
    {
        var a = await Actor(ct); if (!a.HRWrite) throw new UnauthorizedAccessException(); a.RequireSite(siteId);
        if (!await db.BelongSites.AnyAsync(x => x.Id == siteId && x.IsActive, ct)) throw new InvalidOperationException("Cơ sở không hợp lệ.");
        return a;
    }
    [HttpPut("settings/calendar")]
    public async Task<IActionResult> CalendarPolicy(WorkPolicySaveDto input, CancellationToken ct)
    {
        await SettingsActor(input.SiteId, ct);
        var row = await db.WorkCalendarPolicies.SingleOrDefaultAsync(x => x.SiteId == input.SiteId, ct);
        if ((row?.Revision ?? 0) != input.Revision) throw new DbUpdateConcurrencyException("Lịch làm việc đã thay đổi. Hãy tải lại.");
        if (row == null) { row = new WorkCalendarPolicy { SiteId = input.SiteId }; db.Add(row); } else row.Revision++;
        row.WorkingDaysMask = input.WorkingDaysMask; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPut("settings/exceptions")]
    public async Task<IActionResult> CalendarException(CalendarExceptionSaveDto input, CancellationToken ct)
    {
        await SettingsActor(input.SiteId, ct);
        if (input.Date.Year is < 2000 or > 2200) throw new InvalidOperationException("Ngày ngoại lệ không hợp lệ.");
        var row = await db.WorkCalendarExceptions.FindAsync([input.SiteId, input.Date.Date], ct);
        if (row == null) { row = new WorkCalendarException { SiteId = input.SiteId, Date = input.Date.Date }; db.Add(row); }
        row.IsWorkingDay = input.IsWorkingDay; row.Name = input.Name.Trim(); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("settings/exceptions/{siteId:int}/{date}")]
    public async Task<IActionResult> DeleteException(int siteId, DateTime date, CancellationToken ct)
    {
        await SettingsActor(siteId, ct); var row = await db.WorkCalendarExceptions.FindAsync([siteId, date.Date], ct);
        if (row == null) return NotFound(); db.Remove(row); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPut("settings/leave-rules")]
    public async Task<IActionResult> LeaveRule(LeaveRuleSaveDto input, CancellationToken ct)
    {
        await SettingsActor(input.SiteId, ct);
        if (!await db.RequestMasterItems.AnyAsync(x => x.Id == input.LeaveTypeId && x.Category == "LeaveType" && x.IsActive, ct)) throw new InvalidOperationException("Loại nghỉ phép không hợp lệ.");
        var row = await db.LeaveTypePolicies.FindAsync([input.SiteId, input.LeaveTypeId], ct);
        if ((row?.Revision ?? 0) != input.Revision) throw new DbUpdateConcurrencyException("Chính sách đã thay đổi. Hãy tải lại.");
        if (row == null) { row = new LeaveTypePolicy { SiteId = input.SiteId, LeaveTypeId = input.LeaveTypeId }; db.Add(row); } else row.Revision++;
        row.DeductsBalance = input.DeductsBalance; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPut("resources/drivers")]
    public async Task<IActionResult> Driver(ResourceSaveDto input, CancellationToken ct)
    {
        var a = await Actor(ct); if (!a.Dispatch) throw new UnauthorizedAccessException();
        var person = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.EmployeeId && x.IsActive && x.SiteId.HasValue, ct) ?? throw new InvalidOperationException("Nhân viên không hợp lệ.");
        a.RequireSite(person.SiteId.Value);
        var row = await db.DriverProfiles.FindAsync([person.Id], ct);
        if (row == null) { row = new DriverProfile { EmployeeId = person.Id }; db.Add(row); }
        row.IsActive = input.IsActive; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPut("resources/vehicles")]
    public async Task<IActionResult> Fleet(ResourceSaveDto input, CancellationToken ct)
    {
        var a = await Actor(ct); if (!a.Dispatch) throw new UnauthorizedAccessException(); a.RequireSite(input.SiteId);
        if (!await db.BelongSites.AnyAsync(x => x.Id == input.SiteId && x.IsActive, ct) || !await db.RequestMasterItems.AnyAsync(x => x.Id == input.VehicleId && x.Category == "Vehicle" && x.IsActive, ct)) throw new InvalidOperationException("Xe hoặc cơ sở không hợp lệ.");
        var row = await db.FleetVehicles.FindAsync([input.VehicleId.Value], ct);
        if (row != null) { a.RequireSite(row.SiteId); if (row.SiteId != input.SiteId && await db.VehicleDispatches.AnyAsync(x => x.VehicleId == row.VehicleId && (x.Status == DispatchStatus.Assigned || x.Status == DispatchStatus.InProgress), ct)) throw new InvalidOperationException("Xe đang có chuyến được phân công; chưa thể đổi cơ sở."); }
        else { row = new FleetVehicle { VehicleId = input.VehicleId.Value }; db.Add(row); }
        row.SiteId = input.SiteId; row.IsActive = input.IsActive; await db.SaveChangesAsync(ct); return NoContent();
    }
}
