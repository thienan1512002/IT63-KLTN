using System.Data;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow.Operations;

namespace Web.Data.Seeding;

public record AnnualLeaveSeedResult(int Year, int LeaveTypeId, int ActiveUsers, int AddedAccounts, int ExistingAccounts,
    int AddedLeavePolicies, int AddedCalendars, int UsersWithoutSite, int PreservedNonDeductingSites);

/// <summary>Additive demo grants; never resets an HR grant, adjustment, usage or reservation.</summary>
public class AnnualLeaveDemoSeeder(ApplicationDbContext db)
{
    public const decimal AnnualDays = 12;
    public const int DefaultWorkingDays = 0b00111110; // Monday through Friday.
    public const string SeedActor = "seed-annual-leave";

    public async Task<AnnualLeaveSeedResult> SeedAsync(int year, CancellationToken ct = default)
    {
        if (year is < 2000 or > 2200) throw new ArgumentOutOfRangeException(nameof(year));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var type = await db.RequestMasterItems.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Category == "LeaveType" && x.Code == "ANNUAL" && x.IsActive, ct)
            ?? throw new InvalidOperationException("Chưa có loại Nghỉ phép năm (LeaveType/ANNUAL) đang hoạt động.");
        var users = await db.Users.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.SiteId }).ToListAsync(ct);
        var sites = users.Where(x => x.SiteId.HasValue).Select(x => x.SiteId.Value).Distinct().ToList();
        var policies = await db.LeaveTypePolicies.AsNoTracking().Where(x => sites.Contains(x.SiteId) && x.LeaveTypeId == type.Id).ToListAsync(ct);
        var calendars = await db.WorkCalendarPolicies.AsNoTracking().Where(x => sites.Contains(x.SiteId)).Select(x => x.SiteId).ToListAsync(ct);
        var addedPolicies = 0; var addedCalendars = 0;
        foreach (var site in sites)
        {
            if (policies.All(x => x.SiteId != site))
            {
                db.LeaveTypePolicies.Add(new LeaveTypePolicy { SiteId = site, LeaveTypeId = type.Id, DeductsBalance = true });
                addedPolicies++;
            }
            if (!calendars.Contains(site))
            {
                db.WorkCalendarPolicies.Add(new WorkCalendarPolicy { SiteId = site, WorkingDaysMask = DefaultWorkingDays });
                addedCalendars++;
            }
        }
        var existing = (await db.LeaveAccounts.AsNoTracking().Where(x => x.Year == year && x.LeaveTypeId == type.Id)
            .Select(x => x.EmployeeId).ToListAsync(ct)).ToHashSet();
        var added = 0; var preserved = 0; var now = DateTime.UtcNow;
        foreach (var user in users)
        {
            if (existing.Contains(user.Id)) { preserved++; continue; }
            db.LeaveAccounts.Add(new LeaveAccount {
                EmployeeId = user.Id, Year = year, LeaveTypeId = type.Id, AnnualDays = AnnualDays,
                CarryOverDays = 0, Revision = 1, UpdatedAt = now, UpdatedBy = SeedActor
            });
            db.LeaveAdjustments.Add(new LeaveAdjustment {
                Id = Guid.NewGuid(), EmployeeId = user.Id, Year = year, LeaveTypeId = type.Id, Days = 0,
                Actor = SeedActor, At = now, Reason = $"Dữ liệu mẫu: cấp {AnnualDays} ngày nghỉ phép năm {year}; không có phép chuyển năm trước."
            });
            added++;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(year, type.Id, users.Count, added, preserved, addedPolicies, addedCalendars,
            users.Count(x => !x.SiteId.HasValue), policies.Count(x => !x.DeductsBalance));
    }
}
