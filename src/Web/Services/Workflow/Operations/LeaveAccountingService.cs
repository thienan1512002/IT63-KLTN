using System.Data;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Entities.Workflow.Operations;
using Web.Models.Dtos.Workflow;

namespace Web.Services.Workflow.Operations;

public class LeaveAccountingService(ApplicationDbContext db)
{
    private static void HalfDays(decimal days, bool allowNegative = false)
    {
        if (days < -732 || days > 732 || !allowNegative && days < 0 || days != Math.Round(days * 2) / 2)
            throw new InvalidOperationException("Số ngày phép phải theo đơn vị nửa ngày và nằm trong giới hạn cho phép.");
    }
    public async Task<LeaveBalanceDto> BalanceAsync(string employeeId, int year, int leaveTypeId, int? excludeRequestId, CancellationToken ct)
    {
        if (year is < 2000 or > 2200) throw new InvalidOperationException("Năm phép không hợp lệ.");
        var account = await db.LeaveAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year && x.LeaveTypeId == leaveTypeId, ct);
        var site = await db.Users.Where(x => x.Id == employeeId).Select(x => x.SiteId).SingleOrDefaultAsync(ct);
        var policy = await db.LeaveTypePolicies.AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == site && x.LeaveTypeId == leaveTypeId, ct);
        var entries = db.LeaveAllocations.AsNoTracking().Where(x => x.EmployeeId == employeeId && x.Year == year && x.LeaveTypeId == leaveTypeId && x.RequestId != excludeRequestId);
        var used = await entries.Where(x => x.Status == LeaveAllocationStatus.Consumed && x.DeductsBalance).SumAsync(x => (decimal?)x.Days, ct) ?? 0;
        var reserved = await entries.Where(x => x.Status == LeaveAllocationStatus.Reserved && x.DeductsBalance).SumAsync(x => (decimal?)x.Days, ct) ?? 0;
        var unclassified = await entries.CountAsync(x => x.Status != LeaveAllocationStatus.Released && !x.PolicyConfigured, ct);
        var adjustments = await db.LeaveAdjustments.Where(x => x.EmployeeId == employeeId && x.Year == year && x.LeaveTypeId == leaveTypeId).SumAsync(x => (decimal?)x.Days, ct) ?? 0;
        decimal? remaining = account == null ? null : account.AnnualDays + account.CarryOverDays + adjustments - used;
        return new(employeeId, year, leaveTypeId, account != null, account?.AnnualDays ?? 0, account?.CarryOverDays ?? 0, adjustments,
            used, reserved, remaining, remaining - reserved, account?.Revision ?? 0, policy != null, policy?.DeductsBalance ?? false, unclassified);
    }

    public async Task<List<LeaveYearQuoteDto>> QuoteAsync(string employeeId, int leaveTypeId, DateTime start, DateTime end,
        string startSession, string endSession, int? excludeRequestId, CancellationToken ct)
    {
        start = start.Date; end = end.Date;
        if (start.Year < 2000 || end.Year > 2200 || end < start || (end - start).Days >= 366 || startSession is not ("AM" or "PM") || endSession is not ("AM" or "PM") || start == end && startSession == "PM" && endSession == "AM")
            throw new InvalidOperationException("Thời gian nghỉ không hợp lệ hoặc vượt quá 366 ngày.");
        var site = await db.Users.Where(x => x.Id == employeeId).Select(x => x.SiteId).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Nhân viên chưa thuộc cơ sở làm việc.");
        if (!await db.RequestMasterItems.AnyAsync(x => x.Id == leaveTypeId && x.Category == "LeaveType", ct))
            throw new InvalidOperationException("Loại nghỉ phép không hợp lệ.");
        var policy = await db.WorkCalendarPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == site, ct);
        var exceptions = await db.WorkCalendarExceptions.AsNoTracking().Where(x => x.SiteId == site && x.Date >= start && x.Date <= end).ToDictionaryAsync(x => x.Date, x => x.IsWorkingDay, ct);
        var years = new Dictionary<int, decimal>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var working = exceptions.TryGetValue(day, out var value) ? value : policy == null || (policy.WorkingDaysMask & (1 << (int)day.DayOfWeek)) != 0;
            if (!working) continue;
            var days = 1m;
            if (day == start && startSession == "PM") days -= .5m;
            if (day == end && endSession == "AM") days -= .5m;
            years[day.Year] = years.GetValueOrDefault(day.Year) + days;
        }
        var result = new List<LeaveYearQuoteDto>();
        foreach (var year in years) result.Add(new(year.Key, year.Value, await BalanceAsync(employeeId, year.Key, leaveTypeId, excludeRequestId, ct)));
        return result;
    }

    public async Task ReserveAsync(ApprovalRequest request, CancellationToken ct)
    {
        var business = request.BusinessData;
        if (business?.TemplateCode != "LEAVE") return;
        var quotes = await QuoteAsync(business.EmployeeId, business.LeaveTypeId.Value, business.StartAt.Value, business.EndAt.Value,
            business.StartSession, business.EndSession, request.Id, ct);
        if (quotes.Count == 0 || quotes.Sum(x => x.Days) <= 0) throw new InvalidOperationException("Khoảng nghỉ không có buổi làm việc nào theo lịch đã cấu hình.");
        var site = await db.Users.Where(x => x.Id == business.EmployeeId).Select(x => x.SiteId).SingleAsync(ct);
        foreach (var q in quotes.Where(x => x.Balance.DeductsBalance))
        {
            if (q.Balance.UnclassifiedRequests > 0) throw new InvalidOperationException("HR cần đối soát phép cũ hoặc xử lý các yêu cầu chưa có chính sách trước khi kiểm tra hạn mức mới.");
            if (!await db.WorkCalendarPolicies.AnyAsync(x => x.SiteId == site, ct)) throw new InvalidOperationException("HR cần cấu hình lịch làm việc trước khi kiểm soát phép tồn.");
            if (!q.Balance.Configured) throw new InvalidOperationException($"HR chưa cấp phép năm {q.Year} cho loại nghỉ này.");
            if (q.Balance.AvailableDays < q.Days) throw new InvalidOperationException($"Không đủ phép năm {q.Year} cho khoảng nghỉ đề nghị. Nhân viên hoặc HR cần kiểm tra sổ phép.");
        }
        // Approved/held leave cannot overlap another held/approved leave for the same employee.
        var firstDate = business.StartAt.Value.Date;
        var lastDate = business.EndAt.Value.Date;
        if (await (from a in db.LeaveAllocations join b in db.RequestBusinessData on a.RequestId equals b.RequestId
            where a.EmployeeId == business.EmployeeId && a.RequestId != request.Id && a.Status != LeaveAllocationStatus.Released &&
                (b.StartAt < lastDate || b.StartAt == lastDate && (business.EndSession == "PM" || b.StartSession == "AM")) &&
                (b.EndAt > firstDate || b.EndAt == firstDate && (business.StartSession == "AM" || b.EndSession == "PM"))
            select a.RequestId).AnyAsync(ct)) throw new InvalidOperationException("Nhân viên đã có yêu cầu nghỉ trong khoảng thời gian này.");
        var existing = await db.LeaveAllocations.Where(x => x.RequestId == request.Id).ToListAsync(ct);
        foreach (var row in existing.Where(x => quotes.All(q => q.Year != x.Year))) row.Status = LeaveAllocationStatus.Released;
        foreach (var quote in quotes)
        {
            var row = existing.SingleOrDefault(x => x.Year == quote.Year);
            if (row == null) { row = new LeaveAllocation { RequestId = request.Id, Year = quote.Year }; db.LeaveAllocations.Add(row); }
            row.EmployeeId = business.EmployeeId; row.LeaveTypeId = business.LeaveTypeId.Value; row.Days = quote.Days;
            row.PolicyConfigured = quote.Balance.PolicyConfigured; row.DeductsBalance = quote.Balance.DeductsBalance;
            row.Status = LeaveAllocationStatus.Reserved;
        }
    }

    public async Task ReleaseAsync(int requestId, CancellationToken ct)
    {
        foreach (var row in await db.LeaveAllocations.Where(x => x.RequestId == requestId && x.Status == LeaveAllocationStatus.Reserved).ToListAsync(ct))
            row.Status = LeaveAllocationStatus.Released;
    }
    public async Task ConsumeAsync(ApprovalRequest request, CancellationToken ct)
    {
        if (request.BusinessData?.TemplateCode != "LEAVE") return;
        var existing = await db.LeaveAllocations.Where(x => x.RequestId == request.Id).ToListAsync(ct);
        if (existing.Count == 0)
        {
            var b = request.BusinessData;
            var quotes = await QuoteAsync(b.EmployeeId, b.LeaveTypeId.Value, b.StartAt.Value, b.EndAt.Value, b.StartSession ?? "AM", b.EndSession ?? "PM", request.Id, ct);
            foreach (var q in quotes) { var row = new LeaveAllocation { RequestId = request.Id, Year = q.Year, EmployeeId = b.EmployeeId,
                LeaveTypeId = b.LeaveTypeId.Value, Days = q.Days, PolicyConfigured = q.Balance.PolicyConfigured, DeductsBalance = q.Balance.DeductsBalance };
                db.LeaveAllocations.Add(row); existing.Add(row); }
        }
        foreach (var row in existing.Where(x => x.Status != LeaveAllocationStatus.Released)) row.Status = LeaveAllocationStatus.Consumed;
    }

    public async Task SaveAccountAsync(LeaveAccountSaveDto input, OperationsActor actor, CancellationToken ct)
    {
        if (!actor.HRWrite) throw new UnauthorizedAccessException();
        await actor.RequireEmployeeAsync(db, input.EmployeeId, true, false, ct);
        HalfDays(input.AnnualDays); HalfDays(input.CarryOverDays);
        if (input.Year is < 2000 or > 2200 || input.AnnualDays > 366 || input.CarryOverDays > 366 || string.IsNullOrWhiteSpace(input.Reason)) throw new InvalidOperationException("Thông tin cấp phép không hợp lệ.");
        if (!await db.RequestMasterItems.AnyAsync(x => x.Id == input.LeaveTypeId && x.Category == "LeaveType" && x.IsActive, ct)) throw new InvalidOperationException("Chọn loại nghỉ đang hoạt động.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var account = await db.LeaveAccounts.SingleOrDefaultAsync(x => x.EmployeeId == input.EmployeeId && x.Year == input.Year && x.LeaveTypeId == input.LeaveTypeId, ct);
        if ((account?.Revision ?? 0) != input.Revision) throw new DbUpdateConcurrencyException("Sổ phép đã thay đổi. Hãy tải lại.");
        var old = account == null ? "Chưa cấp" : $"Cấp {account.AnnualDays}; chuyển {account.CarryOverDays}";
        if (account == null) { account = new LeaveAccount { EmployeeId = input.EmployeeId, Year = input.Year, LeaveTypeId = input.LeaveTypeId }; db.LeaveAccounts.Add(account); }
        else account.Revision++;
        account.AnnualDays = input.AnnualDays; account.CarryOverDays = input.CarryOverDays; account.UpdatedAt = DateTime.UtcNow; account.UpdatedBy = actor.UserName;
        db.LeaveAdjustments.Add(new LeaveAdjustment { Id = Guid.NewGuid(), EmployeeId = input.EmployeeId, Year = input.Year, LeaveTypeId = input.LeaveTypeId,
            Days = 0, Actor = actor.UserName, Reason = $"{old} → cấp {input.AnnualDays}, chuyển {input.CarryOverDays}. {input.Reason.Trim()}" });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public async Task<int> ReconcileAsync(LeaveReconcileDto input, OperationsActor actor, CancellationToken ct)
    {
        if (!actor.HRWrite) throw new UnauthorizedAccessException();
        await actor.RequireEmployeeAsync(db, input.EmployeeId, true, false, ct);
        var site = await db.Users.Where(x => x.Id == input.EmployeeId).Select(x => x.SiteId).SingleAsync(ct);
        var policy = await db.LeaveTypePolicies.AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == site && x.LeaveTypeId == input.LeaveTypeId, ct)
            ?? throw new InvalidOperationException("Hãy cấu hình cách tính cho loại nghỉ này trước khi đối soát.");
        if (!await db.WorkCalendarPolicies.AnyAsync(x => x.SiteId == site, ct)) throw new InvalidOperationException("Hãy cấu hình lịch làm việc trước khi đối soát dữ liệu cũ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var rows = await db.LeaveAllocations.Where(x => x.EmployeeId == input.EmployeeId && x.Year == input.Year && x.LeaveTypeId == input.LeaveTypeId && !x.PolicyConfigured && x.Status == LeaveAllocationStatus.Consumed).ToListAsync(ct);
        foreach (var row in rows)
        {
            var b = await db.RequestBusinessData.AsNoTracking().SingleAsync(x => x.RequestId == row.RequestId, ct);
            var quotes = await QuoteAsync(row.EmployeeId, row.LeaveTypeId, b.StartAt.Value, b.EndAt.Value, b.StartSession ?? "AM", b.EndSession ?? "PM", row.RequestId, ct);
            row.PreviousDays = row.Days; row.Days = quotes.SingleOrDefault(x => x.Year == row.Year)?.Days ?? 0;
            row.PolicyConfigured = true; row.DeductsBalance = policy.DeductsBalance; row.ClassifiedAt = DateTime.UtcNow;
            row.ClassifiedBy = actor.UserName; row.ClassificationNote = input.Reason.Trim();
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return rows.Count;
    }

    public async Task AdjustAsync(LeaveAdjustmentDto input, OperationsActor actor, CancellationToken ct)
    {
        if (!actor.HRWrite) throw new UnauthorizedAccessException();
        await actor.RequireEmployeeAsync(db, input.EmployeeId, true, false, ct); HalfDays(input.Days, true);
        if (input.Id == Guid.Empty || input.Days == 0 || string.IsNullOrWhiteSpace(input.Reason)) throw new InvalidOperationException("Điều chỉnh cần số ngày khác 0, lý do và mã giao dịch.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var prior = await db.LeaveAdjustments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.Id, ct);
        if (prior != null)
        {
            if (prior.EmployeeId != input.EmployeeId || prior.Year != input.Year || prior.LeaveTypeId != input.LeaveTypeId || prior.Days != input.Days || prior.Reason != input.Reason.Trim())
                throw new InvalidOperationException("Mã giao dịch đã được sử dụng cho nội dung khác.");
            return;
        }
        var account = await db.LeaveAccounts.SingleOrDefaultAsync(x => x.EmployeeId == input.EmployeeId && x.Year == input.Year && x.LeaveTypeId == input.LeaveTypeId, ct)
            ?? throw new InvalidOperationException("Chưa có sổ phép để điều chỉnh.");
        if (account.Revision != input.Revision) throw new DbUpdateConcurrencyException("Sổ phép đã thay đổi. Hãy tải lại.");
        account.Revision++; account.UpdatedAt = DateTime.UtcNow; account.UpdatedBy = actor.UserName;
        db.LeaveAdjustments.Add(new LeaveAdjustment { Id = input.Id, EmployeeId = input.EmployeeId, Year = input.Year, LeaveTypeId = input.LeaveTypeId, Days = input.Days, Reason = input.Reason.Trim(), Actor = actor.UserName });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
}
