using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Data.Seeding;
using Web.Services.Workflow.Core;
using Web.Services.Admin;
using Microsoft.Extensions.Logging.Abstractions;

internal static class UniversitySeedVerification
{
    public static async Task RunAsync(ApplicationDbContext db)
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        db.ChangeTracker.Clear();
        var hasher = new PasswordHasher<AppUser>(); var seeder = new UniversityDemoSeeder(db, hasher);
        var plan = UniversityDemoSeeder.People();
        Check(plan.Count == 200 && plan.Select(x => x.FullName).Distinct().Count() == 200 && plan.Select(x => x.UserName).Distinct().Count() == 200,
            "University plan must contain 200 distinct staff names and login accounts.");
        var beforeCount = await db.Users.CountAsync();
        var originalAdminHash = await db.Users.Where(x => x.Id == IdentityModelSeed.AdminUserId).Select(x => x.PasswordHash).SingleAsync();
        var originalDepartment = await db.Departments.AsNoTracking().SingleAsync(x => x.Id == 1);
        var collision = new AppUser { Id = "test-university-collision", UserName = "uni050", NormalizedUserName = "UNI050", EmpId = "UNIVERSITY-COLLISION", FullName = "Collision fixture", Avatar = "", SiteId = 1, SectionId = 1 };
        db.Users.Add(collision); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        try { await seeder.SeedAsync(UniversityDemoSeeder.DefaultPassword); throw new Exception("Seed must reject another account using the same login."); }
        catch (InvalidOperationException ex) { Check(ex.Message.Contains("trùng dữ liệu khác"), "Collision errors must explain why existing accounts are preserved."); }
        db.ChangeTracker.Clear();
        Check(!await db.BelongSites.AnyAsync(x => x.Code == UniversityDemoSeeder.SiteCode), "A collision must roll back newly inserted campus and organizational data.");
        db.Users.Remove(await db.Users.SingleAsync(x => x.Id == collision.Id)); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await seeder.SeedAsync(UniversityDemoSeeder.DefaultPassword); db.ChangeTracker.Clear();
        Check(result.AddedUsers == 200 && result.AddedDepartments == 20 && result.AddedSections == 40 && result.AddedSites == 1 && result.AddedDrivers == 4,
            "The dataset must create exactly one campus, 20 university units, 40 sections and 200 staff accounts including four registered drivers.");
        var users = await db.Users.AsNoTracking().Where(x => x.SiteId == result.SiteId).Include(x => x.Section).ThenInclude(x => x.Department).ToListAsync();
        Check(users.Count == 200 && users.GroupBy(x => x.Section.DepartmentId).All(x => x.Count() == 10) && users.GroupBy(x => x.SectionId).All(x => x.Count() == 5),
            "Every staff account must belong to the university, with ten per unit and five per section.");
        Check(users.All(x => x.Section.SiteId == result.SiteId && x.Section.Department.SiteId == result.SiteId && x.IsActive && !x.IsOnline),
            "Site, department and section references must agree without pretending seeded accounts are logged in.");
        Check(users.Select(x => x.PasswordHash).Distinct().Count() == 200 && users.Select(x => x.SecurityStamp).Distinct().Count() == 200,
            "Every new account must receive an independent salted password hash and security stamp.");
        foreach (var name in new[] { "uni001", "uni101", "uni068" }) {
            var user = users.Single(x => x.UserName == name);
            Check(hasher.VerifyHashedPassword(user, user.PasswordHash, UniversityDemoSeeder.DefaultPassword) != PasswordVerificationResult.Failed,
                "Rector, lecturer and driver credentials must use the application's Identity password format.");
        }
        var ids = plan.Select(x => x.Id).ToArray();
        Check(await db.UserClaims.CountAsync(x => ids.Contains(x.UserId) && x.ClaimType == "IsFirstTimeLogin" && x.ClaimValue == "true") == 200,
            "All new demo accounts must enter the existing first-login password-change flow.");
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ids.Contains(ur.UserId) select new { ur.UserId, r.Name }).ToListAsync();
        Check(roles.Count(x => x.Name == RoleConstants.Employee) == 200 && !roles.Any(x => x.Name == RoleConstants.SuperUser),
            "University staff must have the employee role without receiving system administrator privileges.");
        Check(roles.Count(x => x.Name == RoleConstants.HRLeaveManage) == 2 && roles.Count(x => x.Name == RoleConstants.HRLeaveView) == 1 && roles.Count(x => x.Name == RoleConstants.TransportDispatch) == 3,
            "The seed must assign HR and dispatcher responsibilities explicitly.");
        var departmentHeads = await db.Departments.AsNoTracking().Where(x => x.SiteId == result.SiteId).ToListAsync();
        var sections = await db.Sections.AsNoTracking().Where(x => x.SiteId == result.SiteId).ToListAsync();
        foreach (var department in departmentHeads) Check(users.Any(x => x.UserName == department.MainApprover && x.Section.DepartmentId == department.Id) && users.Any(x => x.UserName == department.BackupApprover && x.Section.DepartmentId == department.Id), "Every unit must have valid main and backup heads within that unit.");
        foreach (var section in sections) Check(users.Any(x => x.UserName == section.MainApprover && x.SectionId == section.Id) && users.Any(x => x.UserName == section.BackupApprover && x.SectionId == section.Id), "Every section must have valid main and backup heads within that section.");
        var role = await db.ApprovalRoles.AsNoTracking().SingleAsync(x => x.Id == "ROLE-DEPT-MANAGER-ONE");
        var studentServicesEmployee = users.Single(x => x.UserName == "uni043");
        var map = await new ApproverResolver(db, new UserService(db, NullLogger<UserService>.Instance)).BuildStepApproverMapAsync(
            [new() { Id = "university-seed-test-step", ApprovalRole = role }], studentServicesEmployee.UserName, studentServicesEmployee.Section.DepartmentId, studentServicesEmployee.SectionId.Value);
        Check(map["university-seed-test-step"].Single().UserName == "uni041", "Department-based approval rules must resolve the seeded university unit head.");
        db.ChangeTracker.Clear();
        var edited = await db.Users.SingleAsync(x => x.UserName == "uni001"); edited.Position = "Chức vụ đã được quản trị viên hiệu chỉnh";
        var initialHash = edited.PasswordHash; var initialStamp = edited.SecurityStamp;
        var approverRoleId = await db.Roles.Where(x => x.Name == RoleConstants.Approver).Select(x => x.Id).SingleAsync();
        db.UserRoles.Remove(await db.UserRoles.SingleAsync(x => x.UserId == edited.Id && x.RoleId == approverRoleId));
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var repeated = await seeder.SeedAsync("Another@Password2026!"); db.ChangeTracker.Clear();
        edited = await db.Users.SingleAsync(x => x.UserName == "uni001");
        Check(repeated.AddedUsers == 0 && repeated.AddedSites == 0 && repeated.AddedDepartments == 0 && repeated.AddedSections == 0 && repeated.ExistingUsers == 200 && await db.Users.CountAsync() == beforeCount + 200,
            "Rerunning the seed must be idempotent.");
        Check(edited.Position == "Chức vụ đã được quản trị viên hiệu chỉnh" && edited.PasswordHash == initialHash && edited.SecurityStamp == initialStamp,
            "Rerunning must not reset existing profile edits, passwords or live session stamps.");
        Check(!await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == edited.Id && r.Name == RoleConstants.Approver select ur).AnyAsync(),
            "Rerunning must not restore a permission deliberately removed by the administrator.");
        Check(await db.Users.Where(x => x.Id == IdentityModelSeed.AdminUserId).Select(x => x.PasswordHash).SingleAsync() == originalAdminHash &&
            (await db.Departments.AsNoTracking().SingleAsync(x => x.Id == 1)).Name == originalDepartment.Name, "Pre-existing administrator accounts and original departments must remain intact.");
        Console.WriteLine("PASS: University seed, 200 staff, 20 units, 40 sections, valid approval heads, Identity credentials/roles, collision rollback, idempotence and preservation of manual changes.");
    }
}
