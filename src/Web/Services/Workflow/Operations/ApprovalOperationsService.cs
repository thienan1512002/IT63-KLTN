using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Entities.Workflow.Operations;
using Web.Data.Enums.Workflow;

namespace Web.Services.Workflow.Operations;

public class ApprovalOperationsService(ApplicationDbContext db, LeaveAccountingService leave)
{
    public Task SubmittedAsync(ApprovalRequest request, CancellationToken ct) => leave.ReserveAsync(request, ct);
    public Task ReleasedAsync(int requestId, CancellationToken ct) => leave.ReleaseAsync(requestId, ct);

    public async Task CompletedAsync(ApprovalRequest request, CancellationToken ct)
    {
        if (request.Status != RequestStatus.Completed || request.OperationsSyncedAt.HasValue) return;
        var b = request.BusinessData;
        if (b == null || b.TemplateCode is not ("BUSINESS_TRIP" or "LEAVE" or "VEHICLE" or "OVERTIME")) return;
        if (!b.StartAt.HasValue || !b.EndAt.HasValue || !request.SiteId.HasValue)
            throw new InvalidOperationException("Yêu cầu chưa có thời gian hoặc cơ sở để ghi nhận vận hành.");
        var kind = b.TemplateCode switch { "BUSINESS_TRIP" => WorkEventKind.BusinessTrip, "LEAVE" => WorkEventKind.Leave, "VEHICLE" => WorkEventKind.VehiclePassenger, _ => WorkEventKind.Overtime };
        var allDay = kind == WorkEventKind.BusinessTrip;
        var start = b.StartAt.Value; var end = b.EndAt.Value;
        if (allDay) { start = start.Date; end = end.Date.AddDays(1); }
        if (kind == WorkEventKind.Leave) { start = start.Date.AddHours(b.StartSession == "PM" ? 12 : 0); end = end.Date.AddHours(b.EndSession == "AM" ? 12 : 24); allDay = b.StartSession != "PM" && b.EndSession != "AM"; }
        var employees = b.Participants.Select(x => x.EmployeeId).Append(b.EmployeeId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (kind == WorkEventKind.Leave) employees = string.IsNullOrWhiteSpace(b.EmployeeId) ? [] : [b.EmployeeId];
        var existing = await db.EmployeeWorkEvents.Where(x => x.RequestId == request.Id && x.Kind == kind).Select(x => x.EmployeeId).ToListAsync(ct);
        var location = kind == WorkEventKind.VehiclePassenger ? $"{b.PickupLocation} → {b.Destination}" : b.Location;
        foreach (var employee in employees.Except(existing)) db.EmployeeWorkEvents.Add(new EmployeeWorkEvent {
            RequestId = request.Id, EmployeeId = employee, Kind = kind, Title = request.Title, Location = location,
            StartAt = start, EndAt = end, AllDay = allDay
        });
        if (kind == WorkEventKind.Leave) await leave.ConsumeAsync(request, ct);
        if (kind == WorkEventKind.VehiclePassenger && !await db.VehicleDispatches.AnyAsync(x => x.RequestId == request.Id, ct))
        {
            db.VehicleDispatches.Add(new VehicleDispatch { RequestId = request.Id, SiteId = request.SiteId.Value,
                StartAt = start, EndAt = end, Status = DispatchStatus.Waiting, UpdatedBy = "workflow" });
            db.DispatchHistories.Add(new DispatchHistory { RequestId = request.Id, PreviousStatus = DispatchStatus.Waiting,
                NewStatus = DispatchStatus.Waiting, Actor = "workflow", Note = "Yêu cầu đã hoàn tất phê duyệt, chờ điều phối xe và tài xế." });
        }
        request.OperationsSyncedAt ??= DateTime.UtcNow;
    }

    public async Task<object> BackfillAsync(OperationsActor actor, CancellationToken ct)
    {
        if (!actor.Dispatch && !actor.HRWrite) throw new UnauthorizedAccessException();
        var codes = actor.IsAdmin || actor.HRWrite ? new[] { "BUSINESS_TRIP", "LEAVE", "VEHICLE", "OVERTIME" } : ["VEHICLE"];
        if (!actor.Dispatch) codes = codes.Where(x => x != "VEHICLE").ToArray();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var query = db.ApprovalRequests.Where(x => x.Status == RequestStatus.Completed && !x.IsRemoved && x.OperationsSyncedAt == null &&
            (actor.IsAdmin || actor.SiteId.HasValue && x.SiteId == actor.SiteId) && x.BusinessData != null && codes.Contains(x.BusinessData.TemplateCode) && x.BusinessData.StartAt != null && x.BusinessData.EndAt != null);
        var requests = await query.Include(x => x.BusinessData).ThenInclude(x => x.Participants).OrderBy(x => x.Id).Take(200).ToListAsync(ct);
        foreach (var request in requests) { await CompletedAsync(request, ct); await db.SaveChangesAsync(ct); }
        var remaining = await query.CountAsync(ct);
        await transaction.CommitAsync(ct);
        return new { Synced = requests.Count, Remaining = remaining };
    }
}
