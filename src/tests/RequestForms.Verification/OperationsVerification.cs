using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Common;
using Web.Controllers.Api.Workflow;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Entities.Workflow.Operations;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Admin;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Web.Services.Workflow.Operations;

internal static class OperationsVerification
{
    public static async Task RunAsync(ApplicationDbContext db)
    {
        void Check(bool value, string text) { if (!value) throw new Exception(text); }
        async Task Fails<T>(Func<Task> run, string text) where T : Exception { try { await run(); } catch (T) { db.ChangeTracker.Clear(); return; } throw new Exception(text); }
        db.ChangeTracker.Clear();
        var employee = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "employee1");
        var manager = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "manager1");
        var director = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "director1");
        var offsite = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "offsite-test");
        var admin = new OperationsActor(employee.Id, employee.UserName, 1, true, true, true, true);
        var hr = admin with { IsAdmin = false, Dispatch = false };
        var coordinator = admin with { IsAdmin = false, HRRead = false, HRWrite = false };
        var normal = coordinator with { Dispatch = false };
        var leave = new LeaveAccountingService(db); var operations = new ApprovalOperationsService(db, leave); var transport = new VehicleDispatchService(db);
        var resolver = new ApproverResolver(db, new UserService(db, NullLogger<UserService>.Instance));
        var requests = new RequestService(db, null!, new RequestBusinessService(db), new ApprovalWorkflowBuilder(db, resolver), new ApprovalValidator(resolver),
            new ApprovalActionProcessor(resolver), new RequestNumberGenerator(db), new SilentNotifier(), operations);
        var activeType = await db.RequestMasterItems.FindAsync(1); activeType.IsActive = true;
        db.WorkCalendarPolicies.Add(new() { SiteId = 1, WorkingDaysMask = 62 });
        db.LeaveTypePolicies.AddRange(new LeaveTypePolicy { SiteId = 1, LeaveTypeId = 1, DeductsBalance = true }, new LeaveTypePolicy { SiteId = 1, LeaveTypeId = 2, DeductsBalance = false });
        var newTypes = new Dictionary<string, RequestType>();
        foreach (var code in new[] { "LEAVE", "BUSINESS_TRIP", "VEHICLE" }) {
            var type = new RequestType { Code = "OPS_" + code, TemplateCode = code, Name = "Kiểm thử " + code, ApprovalCaseId = "WF-ONE", RequireReason = true };
            db.RequestTypes.Add(type); newTypes[code] = type;
        }
        var car = new RequestMasterItem { Category = "Vehicle", Code = "OPS_CAR_A", Name = "Xe kiểm thử A", ParentId = 111, Capacity = 6 };
        var car2 = new RequestMasterItem { Category = "Vehicle", Code = "OPS_CAR_B", Name = "Xe kiểm thử B", ParentId = 111, Capacity = 6 };
        db.RequestMasterItems.AddRange(car, car2); await db.SaveChangesAsync();
        db.FleetVehicles.AddRange(new FleetVehicle { VehicleId = car.Id, SiteId = 1 }, new FleetVehicle { VehicleId = car2.Id, SiteId = 1 });
        db.DriverProfiles.AddRange(new DriverProfile { EmployeeId = manager.Id }, new DriverProfile { EmployeeId = director.Id }); await db.SaveChangesAsync();
        var ids = newTypes.ToDictionary(x => x.Key, x => x.Value.Id); db.ChangeTracker.Clear();
        async Task<int> Draft(string code, RequestBusinessDataDto input, string title = "Kiểm thử vận hành") {
            var id = await requests.CreateDraftAsync(new() { RequestTypeId = ids[code], Title = title, Content = "Nội dung kiểm thử", Reason = "Kiểm thử", BusinessData = input }, "employee1", []);
            db.ChangeTracker.Clear(); return id;
        }
        async Task Submit(int id) { await requests.SubmitAsync(id, "employee1"); db.ChangeTracker.Clear(); }
        async Task Complete(int id) { await Submit(id); await requests.ApproveAsync(id, "manager1", "Đồng ý"); db.ChangeTracker.Clear(); }
        async Task<ApprovalRequest> Read(int id) => await db.ApprovalRequests.AsNoTracking().Include(x => x.BusinessData).ThenInclude(x => x.Participants).SingleAsync(x => x.Id == id);
        RequestBusinessDataDto Leave(DateTime start, DateTime end, int type = 1, string person = null) => new() { EmployeeId = person ?? employee.Id, LeaveTypeId = type, StartAt = start, EndAt = end, StartSession = "AM", EndSession = "PM" };
        RequestBusinessDataDto Ride(DateTime start, DateTime end) => new() { EmployeeId = employee.Id, ContactPhone = "0900000000", VehicleTypeId = 111, PassengerCount = 1, ParticipantIds = [employee.Id], PickupLocation = "Văn phòng", Destination = "Nhà máy", StartAt = start, EndAt = end };
        async Task Grant(int year, int type = 1, decimal days = 12) { await leave.SaveAccountAsync(new() { EmployeeId = employee.Id, Year = year, LeaveTypeId = type, AnnualDays = days, CarryOverDays = 2, Revision = 0, Reason = "Cấp hạn mức kiểm thử" }, hr, default); db.ChangeTracker.Clear(); }
        await Grant(2026); await Grant(2027);
        var half = await leave.QuoteAsync(employee.Id, 1, new(2026, 10, 23), new(2026, 10, 26), "PM", "AM", null, default);
        Check(half.Single().Days == 1, "Leave quotes must count half-days and skip configured weekends.");
        db.WorkCalendarExceptions.AddRange(new WorkCalendarException { SiteId = 1, Date = new(2026, 10, 26), IsWorkingDay = false, Name = "Nghỉ cơ sở" }, new WorkCalendarException { SiteId = 1, Date = new(2026, 10, 24), IsWorkingDay = true, Name = "Làm bù" }); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        half = await leave.QuoteAsync(employee.Id, 1, new(2026, 10, 23), new(2026, 10, 26), "PM", "AM", null, default);
        Check(half.Single().Days == 1.5m, "Calendar date exceptions must override weekly working days.");
        var crossYear = await leave.QuoteAsync(employee.Id, 1, new(2026, 12, 31), new(2027, 1, 2), "AM", "PM", null, default);
        Check(crossYear.Count == 2 && crossYear.All(x => x.Days == 1), "Leave spanning New Year must use separate annual accounts.");
        var leaveId = await Draft("LEAVE", Leave(new(2026, 11, 2), new(2026, 11, 3))); await Submit(leaveId);
        var balance = await leave.BalanceAsync(employee.Id, 2026, 1, null, default);
        Check(balance.ReservedDays == 2 && balance.UsedDays == 0 && balance.AvailableDays == 12 && !await db.EmployeeWorkEvents.AnyAsync(x => x.RequestId == leaveId), "Submitting must reserve leave without publishing an approved calendar event.");
        await requests.ReturnAsync(leaveId, "manager1", "Bổ sung bàn giao"); db.ChangeTracker.Clear();
        Check((await leave.BalanceAsync(employee.Id, 2026, 1, null, default)).ReservedDays == 0, "Returned leave must release its reservation.");
        await Submit(leaveId); await requests.ApproveAsync(leaveId, "manager1", "Đồng ý"); db.ChangeTracker.Clear();
        balance = await leave.BalanceAsync(employee.Id, 2026, 1, null, default);
        Check(balance.UsedDays == 2 && balance.ReservedDays == 0 && balance.RemainingDays == 12, "Final approval must atomically consume held leave once.");
        await operations.CompletedAsync(await Read(leaveId), default); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Check(await db.EmployeeWorkEvents.CountAsync(x => x.RequestId == leaveId) == 1 && (await leave.BalanceAsync(employee.Id, 2026, 1, null, default)).UsedDays == 2, "Repeated completion synchronization must not duplicate events or leave usage.");
        var overlap = await Draft("LEAVE", Leave(new(2026, 11, 2), new(2026, 11, 2)));
        await Fails<InvalidOperationException>(() => Submit(overlap), "Overlapping held or approved leave must be rejected.");
        Check((await Read(overlap)).Status == RequestStatus.Draft && !await db.LeaveAllocations.AnyAsync(x => x.RequestId == overlap), "A failed submission must roll back workflow status and reservations.");
        var insufficient = await Draft("LEAVE", Leave(new(2026, 12, 1), new(2026, 12, 31)));
        await Fails<InvalidOperationException>(() => Submit(insufficient), "Requests exceeding available annual leave must be rejected.");
        var cancelled = await Draft("LEAVE", Leave(new(2026, 11, 5), new(2026, 11, 5))); await Submit(cancelled); await requests.CancelAsync(cancelled, "employee1", "Không nghỉ nữa"); db.ChangeTracker.Clear();
        var rejected = await Draft("LEAVE", Leave(new(2026, 11, 6), new(2026, 11, 6))); await Submit(rejected); await requests.RejectAsync(rejected, "manager1", "Không phù hợp"); db.ChangeTracker.Clear();
        Check((await leave.BalanceAsync(employee.Id, 2026, 1, null, default)).ReservedDays == 0, "Cancel and rejection must release all held leave.");
        var delta = new LeaveAdjustmentDto { Id = Guid.NewGuid(), EmployeeId = employee.Id, Year = 2026, LeaveTypeId = 1, Days = 1.5m, Revision = balance.Revision, Reason = "Cộng bổ sung đã xác minh" };
        await leave.AdjustAsync(delta, hr, default); db.ChangeTracker.Clear(); await leave.AdjustAsync(delta, hr, default); db.ChangeTracker.Clear();
        Check((await leave.BalanceAsync(employee.Id, 2026, 1, null, default)).Adjustments == 1.5m, "Repeated adjustment requests must be idempotent.");
        await Fails<DbUpdateConcurrencyException>(() => leave.AdjustAsync(new() { Id = Guid.NewGuid(), EmployeeId = employee.Id, Year = 2026, LeaveTypeId = 1, Days = 1, Revision = 1, Reason = "Stale" }, hr, default), "Stale account edits must be rejected.");
        await Fails<UnauthorizedAccessException>(() => hr.RequireEmployeeAsync(db, offsite.Id, true, false, default), "HR scope must not cross sites.");
        await Fails<UnauthorizedAccessException>(() => coordinator.RequireEmployeeAsync(db, manager.Id, true, false, default), "Dispatch permission must not expose other people's leave balances.");
        var snapshotId = await Draft("LEAVE", Leave(new(2026, 11, 20), new(2026, 11, 23))); await Submit(snapshotId);
        var policy = await db.WorkCalendarPolicies.SingleAsync(x => x.SiteId == 1); policy.WorkingDaysMask = 127; policy.Revision++; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await requests.ApproveAsync(snapshotId, "manager1", "Đồng ý"); db.ChangeTracker.Clear();
        Check((await db.LeaveAllocations.SingleAsync(x => x.RequestId == snapshotId)).Days == 2, "Changing the working calendar must not recalculate an existing submitted leave request."); db.ChangeTracker.Clear();
        var trip = await Draft("BUSINESS_TRIP", new() { EmployeeId = employee.Id, ParticipantIds = [manager.Id, director.Id], PurposeId = 10, CountryId = 20, CityId = 30, Location = "Hà Nội", StartAt = new(2026, 11, 10), EndAt = new(2026, 11, 11), CurrencyId = 40, ExchangeRate = 1 });
        await Submit(trip); Check(!await db.EmployeeWorkEvents.AnyAsync(x => x.RequestId == trip), "Partial approval must not publish work calendars.");
        await requests.ApproveAsync(trip, "manager1", "Đồng ý"); db.ChangeTracker.Clear();
        var tripEvents = await db.EmployeeWorkEvents.Where(x => x.RequestId == trip).ToListAsync();
        Check(tripEvents.Count == 3 && tripEvents.All(x => x.AllDay && x.EndAt == new DateTime(2026, 11, 12)), "Approved trips must populate every participant's calendar with an exclusive all-day end."); db.ChangeTracker.Clear();
        await requests.GetDetailsAsync(trip, "director1", false); // Participant, not requester or approver.
        var a = await Draft("VEHICLE", Ride(new(2031, 1, 2, 10, 0, 0), new(2031, 1, 2, 12, 0, 0))); await Complete(a);
        var suggestedRide = Ride(new(2031, 1, 2, 11, 0, 0), new(2031, 1, 2, 13, 0, 0)); suggestedRide.VehicleId = car2.Id;
        var b = await Draft("VEHICLE", suggestedRide); await Complete(b);
        var c = await Draft("VEHICLE", Ride(new(2031, 1, 2, 12, 0, 0), new(2031, 1, 2, 14, 0, 0))); await Complete(c);
        Check((await db.VehicleDispatches.AsNoTracking().SingleAsync(x => x.RequestId == a)).Status == DispatchStatus.Waiting, "Approved vehicle requests must enter a dispatch queue without inventing an assignment.");
        DispatchSaveDto Assignment(int vehicle, string driver, DateTime start, DateTime end, int revision = 1) => new() { Revision = revision, VehicleId = vehicle, DriverEmployeeId = driver, StartAt = start, EndAt = end, Notes = "Điều phối kiểm thử" };
        await transport.AssignAsync(a, Assignment(car.Id, manager.Id, new(2031, 1, 2, 10, 0, 0), new(2031, 1, 2, 12, 0, 0)), coordinator, default); db.ChangeTracker.Clear();
        await Fails<InvalidOperationException>(() => transport.AssignAsync(b, Assignment(car.Id, director.Id, new(2031, 1, 2, 11, 0, 0), new(2031, 1, 2, 13, 0, 0)), coordinator, default), "A vehicle cannot be double-booked.");
        await Fails<InvalidOperationException>(() => transport.AssignAsync(b, Assignment(car2.Id, manager.Id, new(2031, 1, 2, 11, 0, 0), new(2031, 1, 2, 13, 0, 0)), coordinator, default), "A driver cannot be double-booked across vehicles.");
        await transport.AssignAsync(b, Assignment(car2.Id, director.Id, new(2031, 1, 2, 11, 0, 0), new(2031, 1, 2, 13, 0, 0)), coordinator, default); db.ChangeTracker.Clear();
        await transport.AssignAsync(c, Assignment(car.Id, manager.Id, new(2031, 1, 2, 12, 0, 0), new(2031, 1, 2, 14, 0, 0)), coordinator, default); db.ChangeTracker.Clear();
        var grab = await Draft("VEHICLE", Ride(new(2031, 1, 4, 10, 0, 0), new(2031, 1, 4, 12, 0, 0))); await Complete(grab);
        await transport.AssignAsync(grab, new() { Revision=1, UseGrab=true, StartAt=new(2031,1,4,10,0,0), EndAt=new(2031,1,4,12,0,0) }, coordinator, default); db.ChangeTracker.Clear();
        var grabJob = await db.VehicleDispatches.AsNoTracking().SingleAsync(x=>x.RequestId==grab);
        Check(grabJob.UseGrab && grabJob.VehicleId==null && grabJob.DriverEmployeeId==null && grabJob.Status==DispatchStatus.Assigned &&
            !await db.EmployeeWorkEvents.AnyAsync(x=>x.RequestId==grab && x.Kind==WorkEventKind.Driving) &&
            await db.DispatchHistories.AnyAsync(x=>x.RequestId==grab && x.Note.Contains("Grab")),
            "Grab must be persisted and audited without assigning an internal driver or creating a driving event.");
        await transport.AssignAsync(grab, Assignment(car.Id,manager.Id,new(2031,1,4,10,0,0),new(2031,1,4,12,0,0),2),coordinator,default); db.ChangeTracker.Clear();
        Check(!(await db.VehicleDispatches.AsNoTracking().SingleAsync(x=>x.RequestId==grab)).UseGrab &&
            await db.EmployeeWorkEvents.AnyAsync(x=>x.RequestId==grab && x.Kind==WorkEventKind.Driving), "Changing Grab to an internal vehicle must restore normal dispatch.");
        await transport.AssignAsync(grab, new() { Revision=3, UseGrab=true, StartAt=new(2031,1,4,10,0,0), EndAt=new(2031,1,4,12,0,0) },coordinator,default); db.ChangeTracker.Clear();
        Check(!await db.EmployeeWorkEvents.AnyAsync(x=>x.RequestId==grab && x.Kind==WorkEventKind.Driving), "Changing an internal assignment to Grab must remove its driving event.");
        await Fails<DbUpdateConcurrencyException>(() => transport.AssignAsync(a, Assignment(car.Id, manager.Id, new(2031, 1, 2, 10, 0, 0), new(2031, 1, 2, 12, 0, 0)), coordinator, default), "Stale dispatch edits must not overwrite an existing assignment.");
        Check((await Read(a)).BusinessData.VehicleId == null && (await Read(a)).BusinessData.DriverEmployeeId == null, "Actual dispatch must preserve the approved form's original suggestions.");
        await transport.TransitionAsync(b, new() { Revision = 2, Status = DispatchStatus.Cancelled, Notes = "Hủy chuyến kiểm thử" }, coordinator, default); db.ChangeTracker.Clear();
        Check(!await db.EmployeeWorkEvents.AnyAsync(x => x.RequestId == b), "Cancelling a trip must release driver and passenger calendars.");
        var rebooked = await Draft("VEHICLE", suggestedRide); await Submit(rebooked);
        Check((await Read(rebooked)).Status == RequestStatus.Ongoing, "Cancelled dispatches must not keep a vehicle locked through the approved form's old suggestion.");
        var driverActor = normal with { UserId = manager.Id, UserName = manager.UserName };
        await transport.TransitionAsync(a, new() { Revision = 2, Status = DispatchStatus.InProgress }, driverActor, default); db.ChangeTracker.Clear();
        await transport.TransitionAsync(a, new() { Revision = 3, Status = DispatchStatus.Completed }, driverActor, default); db.ChangeTracker.Clear();
        await Fails<InvalidOperationException>(() => transport.TransitionAsync(a, new() { Revision = 4, Status = DispatchStatus.Cancelled, Notes = "Không hợp lệ" }, coordinator, default), "Completed trips must not return to a dispatch or cancellation state.");
        var driverLeave = await Draft("LEAVE", Leave(new(2031, 1, 3), new(2031, 1, 3), 2, manager.Id), "PRIVATE DRIVER LEAVE"); await Complete(driverLeave);
        var blocked = await Draft("VEHICLE", Ride(new(2031, 1, 3, 8, 0, 0), new(2031, 1, 3, 9, 0, 0))); await Complete(blocked);
        await Fails<InvalidOperationException>(() => transport.AssignAsync(blocked, Assignment(car.Id, manager.Id, new(2031, 1, 3, 8, 0, 0), new(2031, 1, 3, 9, 0, 0)), coordinator, default), "Approved leave must make the driver unavailable.");
        OperationsApiController Controller(string name, params string[] roles) => new(db, leave, operations, transport) { ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }.Concat(roles.Select(r => new Claim(ClaimTypes.Role, r))), "Test")) } } };
        JsonElement Json(IActionResult value) => JsonSerializer.SerializeToElement(((OkObjectResult)value).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var selfCalendar = Json(await Controller("employee1").Calendar(new(2026, 11, 1), new(2026, 11, 30), null, null, default));
        Check(selfCalendar.GetProperty("events").EnumerateArray().All(x => x.GetProperty("employeeId").GetString() == employee.Id), "Normal calendar readers must see only their own events.");
        var busyCalendar = Json(await Controller("employee1", RoleConstants.TransportDispatch).Calendar(new(2031, 1, 3), new(2031, 1, 3), manager.Id, null, default));
        Check(busyCalendar.GetProperty("events").EnumerateArray().Any(x => x.GetProperty("title").GetString() == "Không khả dụng") && !busyCalendar.ToString().Contains("PRIVATE DRIVER LEAVE"), "Dispatchers must see driver availability without private leave details.");
        await Fails<UnauthorizedAccessException>(async () => await Controller("employee1", RoleConstants.TransportDispatch).Balances(manager.Id, 2026, default), "Dispatch API claims must not expose staff leave accounts.");
        var dispatchJson = Json(await Controller("employee1", RoleConstants.TransportDispatch).Dispatches(new(2031, 1, 1), new(2031, 1, 31), null, null, null, null, default));
        Check(dispatchJson.GetProperty("items").EnumerateArray().Any(x => x.GetProperty("requestId").GetInt32() == a && x.GetProperty("driver").GetString() == manager.FullName), "Dispatch report must query real assignments.");
        var unknown = await Draft("LEAVE", Leave(new(2026, 11, 13), new(2026, 11, 15), 3)); await Complete(unknown);
        Check(!(await db.LeaveAllocations.AsNoTracking().SingleAsync(x => x.RequestId == unknown)).PolicyConfigured, "Unconfigured leave types must be explicitly marked for reconciliation.");
        db.LeaveTypePolicies.Add(new() { SiteId = 1, LeaveTypeId = 3, DeductsBalance = true });
        policy = await db.WorkCalendarPolicies.SingleAsync(x => x.SiteId == 1); policy.WorkingDaysMask = 62; policy.Revision++; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await Grant(2026, 3, 2);
        await leave.ReconcileAsync(new() { EmployeeId = employee.Id, Year = 2026, LeaveTypeId = 3, Reason = "Đối soát sau thiết lập" }, hr, default); db.ChangeTracker.Clear();
        var reconciled = await db.LeaveAllocations.AsNoTracking().SingleAsync(x => x.RequestId == unknown);
        Check(reconciled.Days == 1 && reconciled.PreviousDays == 3 && reconciled.PolicyConfigured && reconciled.ClassifiedBy == "employee1", "HR reconciliation must classify legacy leave with a recorded before/after amount and actor.");
        // Two independent database transactions cannot spend the same remaining day.
        await leave.SaveAccountAsync(new() { EmployeeId = employee.Id, Year = 2034, LeaveTypeId = 1, AnnualDays = 1, CarryOverDays = 0, Revision = 0, Reason = "Kiểm thử đồng thời" }, hr, default); db.ChangeTracker.Clear();
        var days2034 = Enumerable.Range(0, 20).Select(i => new DateTime(2034, 1, 1).AddDays(i)).Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)).Take(2).ToArray();
        var concurrentA = await Draft("LEAVE", Leave(days2034[0], days2034[0]));
        var concurrentB = await Draft("LEAVE", Leave(days2034[1], days2034[1]));
        var sharedOptions = (DbContextOptions<ApplicationDbContext>)Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions>(db);
        async Task<bool> ConcurrentSubmit(int id)
        {
            await using var isolated = new ApplicationDbContext(sharedOptions);
            var isolatedResolver = new ApproverResolver(isolated, new UserService(isolated, NullLogger<UserService>.Instance));
            var isolatedLeave = new LeaveAccountingService(isolated);
            var isolatedRequests = new RequestService(isolated, null!, new RequestBusinessService(isolated), new ApprovalWorkflowBuilder(isolated, isolatedResolver),
                new ApprovalValidator(isolatedResolver), new ApprovalActionProcessor(isolatedResolver), new RequestNumberGenerator(isolated), new SilentNotifier(), new ApprovalOperationsService(isolated, isolatedLeave));
            try { await isolatedRequests.SubmitAsync(id, "employee1"); return true; }
            catch (Exception ex)
            {
                Exception current = ex;
                while (current != null) { if (current is Microsoft.Data.SqlClient.SqlException { Number: 1205 }) return false; current = current.InnerException; }
                if (ex is InvalidOperationException && ex.Message.StartsWith("Không đủ phép")) return false;
                throw;
            }
        }
        var concurrentResults = await Task.WhenAll(ConcurrentSubmit(concurrentA), ConcurrentSubmit(concurrentB));
        Check(concurrentResults.Count(x => x) == 1 && (await leave.BalanceAsync(employee.Id, 2034, 1, null, default)).ReservedDays == 1,
            "Concurrent submissions must never over-reserve the employee's annual leave balance.");
        await operations.BackfillAsync(admin, default); db.ChangeTracker.Clear(); var eventCount = await db.EmployeeWorkEvents.CountAsync(); var allocationCount = await db.LeaveAllocations.CountAsync(); var dispatchCount = await db.VehicleDispatches.CountAsync();
        await operations.BackfillAsync(admin, default); db.ChangeTracker.Clear();
        Check(await db.EmployeeWorkEvents.CountAsync() == eventCount && await db.LeaveAllocations.CountAsync() == allocationCount && await db.VehicleDispatches.CountAsync() == dispatchCount, "Legacy synchronization must be idempotent across calendars, dispatches and leave accounting.");
        Console.WriteLine("PASS: Atomic post-approval calendars, all trip participants, leave reservations/consumption/release, working calendars, half-days, year splits, idempotent adjustments, legacy reconciliation, dispatch assignment/conflicts/lifecycle and scoped privacy.");
    }
    private sealed class SilentNotifier : IApprovalNotifier
    {
        public Task NotifySubmittedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null) => Task.CompletedTask;
        public Task NotifyApprovedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null) => Task.CompletedTask;
        public Task NotifyReturnedOrRejectedAsync(ApprovalActionContext context, bool isRejection = false, Dictionary<string, string> additionalData = null) => Task.CompletedTask;
        public Task DismissNotificationsAsync(string faxCode, string url) => Task.CompletedTask;
    }
}
