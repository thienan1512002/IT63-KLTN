using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow.Operations;
using Web.Data.Seeding;
using Web.Services.Workflow.Operations;

internal static class AnnualLeaveSeedVerification
{
    public static async Task RunAsync(ApplicationDbContext db)
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        db.ChangeTracker.Clear();
        const int year = 2026;
        var type = await db.RequestMasterItems.SingleAsync(x => x.Category == "LeaveType" && x.Code == "ANNUAL");
        var manualUser = await db.Users.SingleAsync(x => x.UserName == "uni101");
        db.LeaveAccounts.Add(new LeaveAccount { EmployeeId=manualUser.Id, Year=year, LeaveTypeId=type.Id,
            AnnualDays=18, CarryOverDays=2, Revision=4, UpdatedBy="hr-manual" });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var calendars = await db.WorkCalendarPolicies.AsNoTracking().ToListAsync();
        var policies = await db.LeaveTypePolicies.AsNoTracking().ToListAsync();
        var allocationCount = await db.LeaveAllocations.CountAsync();
        var seeder = new AnnualLeaveDemoSeeder(db);
        var result = await seeder.SeedAsync(year); db.ChangeTracker.Clear();
        var activeIds = await db.Users.Where(x => x.IsActive).Select(x => x.Id).ToListAsync();
        Check(await db.LeaveAccounts.CountAsync(x => activeIds.Contains(x.EmployeeId) && x.Year==year && x.LeaveTypeId==type.Id)==activeIds.Count,
            "All active users must have an annual-leave ledger for the selected year.");
        var account = await db.LeaveAccounts.AsNoTracking().SingleAsync(x => x.EmployeeId==manualUser.Id && x.Year==year && x.LeaveTypeId==type.Id);
        Check(account.AnnualDays==18 && account.CarryOverDays==2 && account.Revision==4 && account.UpdatedBy=="hr-manual", "HR's existing grant must remain unchanged.");
        Check(await db.LeaveAccounts.CountAsync(x => x.UpdatedBy==AnnualLeaveDemoSeeder.SeedActor && x.AnnualDays==12 && x.CarryOverDays==0)==result.AddedAccounts,
            "New ledgers must contain exactly 12 days with no invented carryover.");
        foreach(var old in calendars) Check(await db.WorkCalendarPolicies.AnyAsync(x=>x.SiteId==old.SiteId && x.WorkingDaysMask==old.WorkingDaysMask && x.Revision==old.Revision),"Existing work calendars must be preserved.");
        foreach(var old in policies) Check(await db.LeaveTypePolicies.AnyAsync(x=>x.SiteId==old.SiteId && x.LeaveTypeId==old.LeaveTypeId && x.DeductsBalance==old.DeductsBalance && x.Revision==old.Revision),"Existing policies must be preserved.");
        Check(await db.LeaveAllocations.CountAsync()==allocationCount,"Seed must not rewrite leave usage or reservations.");
        var person = await db.Users.SingleAsync(x=>x.UserName=="uni002");
        var balance = await new LeaveAccountingService(db).BalanceAsync(person.Id,year,type.Id,null,default);
        Check(balance.Configured && balance.PolicyConfigured && balance.DeductsBalance && balance.AnnualDays==12 && balance.AvailableDays==12,
            "The balance service used by the UI must return 12 available days for a new staff ledger.");
        var auditCount = await db.LeaveAdjustments.CountAsync();
        var again = await seeder.SeedAsync(year); db.ChangeTracker.Clear();
        Check(again.AddedAccounts==0 && again.AddedLeavePolicies==0 && again.AddedCalendars==0 && await db.LeaveAdjustments.CountAsync()==auditCount,
            "Rerunning the seed must not duplicate grants or audit entries.");
        Console.WriteLine("PASS: Annual leave seed, active staff coverage, 12-day UI balances, preserved HR grants/calendars/policies/usage and rerun idempotence.");
    }
}
