using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.Entities.Admin;
using Web.Data.Entities.Identity;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;

namespace Web.Data.Seeding;

public static class ApprovalDemoSeed
{
    private static readonly DateTime SeedUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string EmployeeRoleId = "a0000001-0000-4000-8000-000000000021";
    private const string ApproverRoleId = "a0000001-0000-4000-8000-000000000022";
    private const string EmployeeId = "b0000001-0000-4000-8000-000000000021";
    private const string ManagerId = "b0000001-0000-4000-8000-000000000022";
    private const string DirectorId = "b0000001-0000-4000-8000-000000000023";

    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<AppRole>().HasData(
            Role(EmployeeRoleId, RoleConstants.Employee, "Nhân viên"),
            Role(ApproverRoleId, RoleConstants.Approver, "Người phê duyệt"));

        builder.Entity<Department>().HasData(
            Department(1, "Công nghệ thông tin", "IT", "manager1"),
            Department(2, "Tài chính", "FIN", "director1"),
            Department(3, RoleConstants.BOD, "BOD", "director1"));
        builder.Entity<Section>().HasData(
            Section(1, 1, "IT General", "IT General", "manager1"),
            Section(2, 2, "Finance General", "Finance General", "director1"),
            Section(3, 3, "BOD Office", "BOD Office", "director1"));

        builder.Entity<AppUser>().HasData(
            User(EmployeeId, "employee1", "Nguyễn Văn Nhân Viên", "EMP001", 1),
            User(ManagerId, "manager1", "Trần Thị Trưởng Phòng", "MGR001", 1),
            User(DirectorId, "director1", "Lê Văn Giám Đốc", "DIR001", 3));
        builder.Entity<AppUserRole>().HasData(
            new AppUserRole { UserId = EmployeeId, RoleId = EmployeeRoleId },
            new AppUserRole { UserId = ManagerId, RoleId = EmployeeRoleId },
            new AppUserRole { UserId = ManagerId, RoleId = ApproverRoleId },
            new AppUserRole { UserId = DirectorId, RoleId = ApproverRoleId });
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().HasData(
            new Microsoft.AspNetCore.Identity.IdentityUserClaim<string> { Id = -1, UserId = IdentityModelSeed.AdminUserId, ClaimType = "IsFirstTimeLogin", ClaimValue = "false" },
            new Microsoft.AspNetCore.Identity.IdentityUserClaim<string> { Id = -2, UserId = EmployeeId, ClaimType = "IsFirstTimeLogin", ClaimValue = "false" },
            new Microsoft.AspNetCore.Identity.IdentityUserClaim<string> { Id = -3, UserId = ManagerId, ClaimType = "IsFirstTimeLogin", ClaimValue = "false" },
            new Microsoft.AspNetCore.Identity.IdentityUserClaim<string> { Id = -4, UserId = DirectorId, ClaimType = "IsFirstTimeLogin", ClaimValue = "false" });

        builder.Entity<FormApproverSetting>().HasData(new FormApproverSetting
        {
            Id = 1, Title = "Giám đốc", DisplayTitle = "Giám đốc",
            UserNames = "director1", FormCode = "WF-MULTI", IsRemoved = false
        });
        builder.Entity<ApprovalCase>().HasData(
            ApprovalCase("WF-ONE", "Phê duyệt một bước"),
            ApprovalCase("WF-MULTI", "Phê duyệt hai bước"));
        builder.Entity<ApprovalRole>().HasData(
            ApprovalRole("ROLE-DEPT-MANAGER-ONE", "Trưởng phòng", "WF-ONE", ApprovalRoleType.DepartmentManager),
            ApprovalRole("ROLE-DEPT-MANAGER-MULTI", "Trưởng phòng", "WF-MULTI", ApprovalRoleType.DepartmentManager),
            new ApprovalRole
            {
                Id = "ROLE-DIRECTOR", Title = "Giám đốc", FormCode = "WF-MULTI",
                RoleType = ApprovalRoleType.NormalApprover, FormApprovalId = 1, IsActive = true,
                CreatedBy = "seed", UpdatedBy = "seed", CreatedAt = SeedUtc
            });
        builder.Entity<ApprovalStep>().HasData(
            Step("STEP-ONE-1", "WF-ONE", "ROLE-DEPT-MANAGER-ONE", 1),
            Step("STEP-MULTI-1", "WF-MULTI", "ROLE-DEPT-MANAGER-MULTI", 1, "STEP-MULTI-2"),
            Step("STEP-MULTI-2", "WF-MULTI", "ROLE-DIRECTOR", 2));

        builder.Entity<RequestType>().HasData(
            RequestType(1, "LEAVE", "Đơn xin nghỉ phép", "WF-ONE", true, false, true),
            RequestType(2, "PURCHASE", "Đề nghị mua hàng", "WF-MULTI", true, true, true),
            RequestType(3, "PAYMENT", "Đề nghị thanh toán", "WF-MULTI", true, true, true),
            RequestType(4, "EQUIPMENT", "Yêu cầu cấp thiết bị", "WF-ONE", true, false, true),
            RequestType(5, "OVERTIME", "Yêu cầu làm thêm giờ", "WF-ONE", true, false, true),
            RequestType(6, "INTERNAL", "Đề xuất nội bộ", "WF-MULTI", false, false, true));
    }

    private static AppRole Role(string id, string name, string display) => new()
    {
        Id = id, Name = name, NormalizedName = name.ToUpperInvariant(), DisplayName = display,
        CategoryName = "Phê duyệt", CategoryOrder = 1, ConcurrencyStamp = "seed-" + name.ToLowerInvariant()
    };
    private static Department Department(int id, string name, string shortName, string manager) => new()
    {
        Id = id, Name = name, ShortName = shortName, Description = name, SiteId = 1,
        MainApprover = manager, BackupApprover = string.Empty, IsActive = true, CreatedAt = SeedUtc, CreatedBy = "seed"
    };
    private static Section Section(int id, int departmentId, string name, string engName, string manager) => new()
    {
        Id = id, DepartmentId = departmentId, SiteId = 1, Name = name, EngName = engName,
        ShortName = name, Note = string.Empty, MainApprover = manager, BackupApprover = string.Empty,
        PIC = string.Empty, DeputyApprovers = string.Empty, IsActive = true, CreatedAt = SeedUtc, CreatedBy = "seed"
    };
    private static AppUser User(string id, string userName, string fullName, string empId, int sectionId) => new()
    {
        Id = id, UserName = userName, NormalizedUserName = userName.ToUpperInvariant(),
        Email = $"{userName}@demo.local", NormalizedEmail = $"{userName.ToUpperInvariant()}@DEMO.LOCAL",
        EmailConfirmed = true, PasswordHash = IdentityModelSeed.BootstrapAdminPasswordHash,
        SecurityStamp = "seed-security-" + userName, ConcurrencyStamp = "seed-concurrency-" + userName,
        FullName = fullName, EmpId = empId, Avatar = string.Empty, Position = "Demo",
        SectionId = sectionId, SiteId = 1, IsActive = true, IsOnline = true,
        LockoutEnabled = true, CreatedAt = SeedUtc, HireDate = SeedUtc
    };
    private static ApprovalCase ApprovalCase(string id, string name) => new()
    {
        CaseId = id, CaseName = name, Description = name, BelongSite = "ALL", FormCode = id,
        IsActive = true, CreatedAt = SeedUtc, CreatedBy = "seed", UpdatedBy = "seed"
    };
    private static ApprovalRole ApprovalRole(string id, string title, string formCode, ApprovalRoleType type) => new()
    {
        Id = id, Title = title, FormCode = formCode, RoleType = type, IsActive = true,
        CreatedBy = "seed", UpdatedBy = "seed", CreatedAt = SeedUtc
    };
    private static ApprovalStep Step(string id, string caseId, string roleId, int order, string nextId = null) => new()
    {
        Id = id, ApprovalCaseId = caseId, ApprovalRoleId = roleId, StepOrder = order,
        NextStepId = nextId, ActionTypes = ApprovalActionType.Approve | ApprovalActionType.Reject |
            ApprovalActionType.Return | ApprovalActionType.Comment,
        ApprovalType = ApprovalType.Approval, GroupTitle = string.Empty, IsRemoved = false
    };
    private static RequestType RequestType(int id, string code, string name, string caseId,
        bool date, bool amount, bool reason) => new()
    {
        Id = id, Code = code, TemplateCode = code, Name = name, Description = name, ApprovalCaseId = caseId,
        IsActive = true, RequireRequestedDate = date, RequireAmount = amount,
        RequireReason = reason, AllowAttachment = true, CreatedAt = SeedUtc
    };
}
