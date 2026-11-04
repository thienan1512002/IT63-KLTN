using System.Data;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow.Operations;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;

namespace Web.Services.Workflow.Operations;

public class VehicleDispatchService(ApplicationDbContext db)
{
    public async Task AssignAsync(int requestId, DispatchSaveDto input, OperationsActor actor, CancellationToken ct)
    {
        if (!actor.Dispatch) throw new UnauthorizedAccessException();
        if (input.EndAt <= input.StartAt || (input.EndAt - input.StartAt).TotalDays > 366 || input.StartAt.Year < 2000 || input.EndAt.Year > 2200)
            throw new InvalidOperationException("Thời gian điều xe không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var job = await db.VehicleDispatches.SingleOrDefaultAsync(x => x.RequestId == requestId, ct) ?? throw new KeyNotFoundException("Không tìm thấy chuyến xe.");
        actor.RequireSite(job.SiteId);
        if (job.Revision != input.Revision) throw new DbUpdateConcurrencyException("Chuyến xe đã được cập nhật. Hãy tải lại.");
        if (job.Status is not (DispatchStatus.Waiting or DispatchStatus.Assigned)) throw new InvalidOperationException("Chỉ được điều chỉnh chuyến đang chờ hoặc đã phân công.");
        var request = await db.ApprovalRequests.Include(x => x.BusinessData).SingleAsync(x => x.Id == requestId, ct);
        if (request.Status != RequestStatus.Completed || request.IsRemoved) throw new InvalidOperationException("Chỉ điều xe cho yêu cầu đã hoàn tất phê duyệt.");
        if (input.UseGrab)
        {
            var previous = job.Status;
            job.UseGrab = true; job.VehicleId = null; job.DriverEmployeeId = null;
            job.StartAt = input.StartAt; job.EndAt = input.EndAt; job.Notes = input.Notes?.Trim();
            job.Status = DispatchStatus.Assigned; job.Revision++; job.UpdatedAt = DateTime.UtcNow; job.UpdatedBy = actor.UserName;
            db.EmployeeWorkEvents.RemoveRange(await db.EmployeeWorkEvents.Where(x => x.RequestId == requestId && x.Kind == WorkEventKind.Driving).ToListAsync(ct));
            foreach (var passenger in await db.EmployeeWorkEvents.Where(x => x.RequestId == requestId && x.Kind == WorkEventKind.VehiclePassenger).ToListAsync(ct))
            { passenger.StartAt = input.StartAt; passenger.EndAt = input.EndAt; }
            db.DispatchHistories.Add(new DispatchHistory { RequestId = requestId, PreviousStatus = previous, NewStatus = job.Status,
                Actor = actor.UserName, Note = "Phân công đi Grab. " + job.Notes });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return;
        }
        if (!await (from d in db.DriverProfiles join u in db.Users on d.EmployeeId equals u.Id
            where d.EmployeeId == input.DriverEmployeeId && d.IsActive && u.IsActive && u.SiteId == job.SiteId &&
            (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow) select d.EmployeeId).AnyAsync(ct))
            throw new InvalidOperationException("Chọn tài xế đang hoạt động và thuộc cùng cơ sở.");
        var vehicle = await (from f in db.FleetVehicles join v in db.RequestMasterItems on f.VehicleId equals v.Id
            where f.VehicleId == input.VehicleId && f.SiteId == job.SiteId && f.IsActive && v.IsActive && v.Category == "Vehicle" select v).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Xe chưa được đăng ký vào đội xe của cơ sở hoặc đang ngừng sử dụng.");
        if (vehicle.ParentId != request.BusinessData.VehicleTypeId) throw new InvalidOperationException("Xe phải thuộc loại xe đã được phê duyệt.");
        var capacity = vehicle.Capacity ?? await db.RequestMasterItems.Where(x => x.Id == vehicle.ParentId).Select(x => x.Capacity).SingleOrDefaultAsync(ct);
        if (!capacity.HasValue) throw new InvalidOperationException("Cần khai báo sức chứa của xe trước khi điều phối.");
        if (request.BusinessData.PassengerCount > capacity) throw new InvalidOperationException("Xe không đủ sức chứa cho số hành khách đã được phê duyệt.");
        if (await db.VehicleDispatches.AnyAsync(x => x.RequestId != requestId && x.Status != DispatchStatus.Waiting && x.Status != DispatchStatus.Cancelled &&
            x.StartAt < input.EndAt && x.EndAt > input.StartAt && (x.VehicleId == input.VehicleId || x.DriverEmployeeId == input.DriverEmployeeId), ct))
            throw new InvalidOperationException("Xe hoặc tài xế đã được phân công trong khoảng thời gian này.");
        if (await db.EmployeeWorkEvents.AnyAsync(x => x.EmployeeId == input.DriverEmployeeId && x.RequestId != requestId &&
            (x.Kind == WorkEventKind.Driving || x.Kind == WorkEventKind.Leave || x.Kind == WorkEventKind.BusinessTrip) && x.StartAt < input.EndAt && x.EndAt > input.StartAt, ct))
            throw new InvalidOperationException("Tài xế không khả dụng: đang nghỉ phép, công tác hoặc có lịch lái xe trùng giờ.");
        var old = job.Status;
        var changes = $"Xe {job.VehicleId?.ToString() ?? "—"} → {vehicle.Code}; tài xế {job.DriverEmployeeId ?? "—"} → {input.DriverEmployeeId}.";
        job.UseGrab = false; job.VehicleId = input.VehicleId; job.DriverEmployeeId = input.DriverEmployeeId; job.StartAt = input.StartAt; job.EndAt = input.EndAt;
        job.Notes = input.Notes?.Trim(); job.Status = DispatchStatus.Assigned; job.Revision++; job.UpdatedAt = DateTime.UtcNow; job.UpdatedBy = actor.UserName;
        var oldEvents = await db.EmployeeWorkEvents.Where(x => x.RequestId == requestId && x.Kind == WorkEventKind.Driving).ToListAsync(ct);
        foreach (var obsolete in oldEvents.Where(x => x.EmployeeId != input.DriverEmployeeId)) db.EmployeeWorkEvents.Remove(obsolete);
        var entry = oldEvents.SingleOrDefault(x => x.EmployeeId == input.DriverEmployeeId);
        if (entry == null) { entry = new EmployeeWorkEvent { RequestId = requestId, EmployeeId = input.DriverEmployeeId, Kind = WorkEventKind.Driving }; db.EmployeeWorkEvents.Add(entry); }
        entry.StartAt = input.StartAt; entry.EndAt = input.EndAt; entry.Title = $"Lái xe {vehicle.Code} · {request.Title}";
        entry.Location = $"{request.BusinessData.PickupLocation} → {request.BusinessData.Destination}";
        foreach (var passenger in await db.EmployeeWorkEvents.Where(x => x.RequestId == requestId && x.Kind == WorkEventKind.VehiclePassenger).ToListAsync(ct))
        { passenger.StartAt = input.StartAt; passenger.EndAt = input.EndAt; }
        db.DispatchHistories.Add(new DispatchHistory { RequestId = requestId, PreviousStatus = old, NewStatus = job.Status, VehicleId = job.VehicleId,
            DriverEmployeeId = job.DriverEmployeeId, Actor = actor.UserName, Note = changes + " " + job.Notes });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public async Task TransitionAsync(int requestId, DispatchActionDto input, OperationsActor actor, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var job = await db.VehicleDispatches.SingleOrDefaultAsync(x => x.RequestId == requestId, ct) ?? throw new KeyNotFoundException("Không tìm thấy chuyến xe.");
        actor.RequireSite(job.SiteId);
        if (!actor.Dispatch && job.DriverEmployeeId != actor.UserId) throw new UnauthorizedAccessException();
        if (job.Revision != input.Revision) throw new DbUpdateConcurrencyException("Chuyến xe đã được cập nhật. Hãy tải lại.");
        var allowed = (job.Status, input.Status) switch {
            (DispatchStatus.Assigned, DispatchStatus.InProgress) => true,
            (DispatchStatus.InProgress, DispatchStatus.Completed) => true,
            (DispatchStatus.Waiting or DispatchStatus.Assigned, DispatchStatus.Cancelled) => actor.Dispatch,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException("Không thể chuyển trạng thái chuyến xe theo yêu cầu này.");
        if (input.Status == DispatchStatus.Cancelled && string.IsNullOrWhiteSpace(input.Notes)) throw new InvalidOperationException("Cần ghi lý do hủy chuyến.");
        var old = job.Status; job.Status = input.Status; job.Revision++; job.UpdatedAt = DateTime.UtcNow; job.UpdatedBy = actor.UserName;
        if (input.Status == DispatchStatus.InProgress) job.ActualStartAt = DateTime.UtcNow;
        if (input.Status == DispatchStatus.Completed) job.ActualEndAt = DateTime.UtcNow;
        if (input.Status == DispatchStatus.Cancelled)
            db.EmployeeWorkEvents.RemoveRange(await db.EmployeeWorkEvents.Where(x => x.RequestId == requestId && (x.Kind == WorkEventKind.Driving || x.Kind == WorkEventKind.VehiclePassenger)).ToListAsync(ct));
        db.DispatchHistories.Add(new DispatchHistory { RequestId = requestId, PreviousStatus = old, NewStatus = job.Status,
            VehicleId = job.VehicleId, DriverEmployeeId = job.DriverEmployeeId, Note = input.Notes?.Trim(), Actor = actor.UserName });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
}
