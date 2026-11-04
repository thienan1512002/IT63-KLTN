using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.Entities.Admin;
using Web.Data.Entities.Identity;

namespace Web.Data.Seeding;

/// <summary>Fixed keys for EF <see cref="ModelBuilder.HasData"/> (sites, org, roles, bootstrap admin).</summary>
public static class IdentityModelSeed
{
    public const string SuperUserRoleId = "a0000001-0000-4000-8000-000000000001";
    public const string HrPersonnelViewRoleId = "a0000001-0000-4000-8000-000000000011";
    public const string HrPersonnelManageRoleId = "a0000001-0000-4000-8000-000000000012";

    public const string AdminUserId = "b0000001-0000-4000-8000-000000000001";

    public const int SeedSiteYPFId = 1;


    /// <summary>
    /// ASP.NET Core Identity v3 hash for password <c>Nmv@1231</c> (from <c>PasswordHasher&lt;IdentityUser&gt;</c>, package 10.0.x).
    /// Regenerate if upgrading Identity password format.
    /// </summary>
    public const string BootstrapAdminPasswordHash =
        "AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg==";

    private static readonly DateTime SeedUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<BelongSite>().HasData(
            new BelongSite
            {
                Id = SeedSiteYPFId,
                Name = "Cơ sở điều hành",
                Code = "CS-DH",
                Description = "Cơ sở điều hành",
                IsActive = true,
                CreatedAt = SeedUtc
            });



        builder.Entity<AppRole>().HasData(
            new AppRole
            {
                Id = SuperUserRoleId,
                Name = RoleConstants.SuperUser,
                NormalizedName = "SUPERUSER",
                ConcurrencyStamp = "seed-superuser-stamp-001",
                DisplayName = "Super User",
                CategoryName = "He thong",
                CategoryOrder = 0
            },
            new AppRole
            {
                Id = HrPersonnelViewRoleId,
                Name = RoleConstants.HRPersonnelView,
                NormalizedName = "HR.PERSONNEL.VIEW",
                ConcurrencyStamp = "seed-hr-view-stamp-001",
                DisplayName = "Xem thông tin nhân sự",
                CategoryName = "Quản lý nhân sự",
                CategoryOrder = 1
            },
            new AppRole
            {
                Id = HrPersonnelManageRoleId,
                Name = RoleConstants.HRPersonnelManage,
                NormalizedName = "HR.PERSONNEL.MANAGE",
                ConcurrencyStamp = "seed-hr-manage-stamp-001",
                DisplayName = "Quản lý thông tin nhân sự",
                CategoryName = "Quản lý nhân sự",
                CategoryOrder = 1
            });

        builder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = AdminUserId,
                UserName = "nmvadmin",
                NormalizedUserName = "NMVADMIN",
                Email = "nmvadmin@demo.local",
                NormalizedEmail = "NMVADMIN@DEMO.LOCAL",
                EmailConfirmed = true,
                PasswordHash = BootstrapAdminPasswordHash,
                SecurityStamp = "seed-nmvadmin-security-stamp",
                ConcurrencyStamp = "seed-nmvadmin-concurrency-stamp",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                FullName = "System Administrator",
                EmpId = "ADMIN001",
                Avatar = string.Empty,
                Position = string.Empty,
                IsActive = true,
                IsOnline = false,
                IsOperator = false,
                CreatedAt = SeedUtc,
                HireDate = SeedUtc,
                SectionId = 1,
                SiteId = 1,
            });

        builder.Entity<AppUserRole>().HasData(
            new AppUserRole
            {
                UserId = AdminUserId,
                RoleId = SuperUserRoleId
            });
    }
}
