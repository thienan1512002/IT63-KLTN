using Microsoft.AspNetCore.Identity;
using Web.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Web.Data.Entities.Admin;
using Web.Data.Entities.Identity;
using Web.Data.Entities.Notification;
using Web.Data.Entities.Workflow;
using Web.Data.Entities.Workflow.Operations;
using Web.Data.Seeding;
using Worker.Shared.Entities;

namespace Web.Data.DbContexts;

public class ApplicationDbContext : IdentityDbContext<
    AppUser,
    AppRole,
    string,
    IdentityUserClaim<string>,
    AppUserRole,
    IdentityUserLogin<string>,
    IdentityRoleClaim<string>,
    IdentityUserToken<string>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId).IsRequired();
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId).IsRequired();
        });

        builder.Entity<BelongSite>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Department>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ShortName).HasMaxLength(300);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.MainApprover).HasMaxLength(255);
            entity.HasIndex(x => new { x.SiteId, x.Name }).IsUnique();
            entity.HasOne(x => x.BelongSite).WithMany().HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Section>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.DepartmentId, x.Name }).IsUnique();
            entity.HasOne(x => x.Department).WithMany(x => x.Sections)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BelongSite).WithMany().HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.Position).HasMaxLength(200);
            entity.HasIndex(x => x.EmpId).IsUnique();
            entity.HasOne(x => x.Section).WithMany(x => x.Users)
                .HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BelongSite).WithMany().HasForeignKey(x => x.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RequestType>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasOne(x => x.ApprovalCase).WithMany()
                .HasForeignKey(x => x.ApprovalCaseId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApprovalCase>().Property(x => x.Revision).IsConcurrencyToken();
        builder.Entity<ApprovalWorkflowVersion>(entity =>
        {
            entity.HasKey(x => new { x.ApprovalCaseId, x.Revision });
            entity.HasOne(x => x.ApprovalCase).WithMany().HasForeignKey(x => x.ApprovalCaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ApprovalStep>().HasIndex(x => new { x.ApprovalCaseId, x.Revision, x.StepOrder });

        builder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasIndex(x => x.ApplicationNo).IsUnique();
            entity.HasIndex(x => new { x.CreatedByUserId, x.Status });
            entity.HasIndex(x => new { x.DepartmentId, x.Status });
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.RequestType).WithMany().HasForeignKey(x => x.RequestTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ApprovalCase).WithMany()
                .HasForeignKey(x => x.ApprovalCaseId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApprovalStep>(entity =>
        {
            entity.HasOne(x => x.ReturnStep).WithMany().HasForeignKey(x => x.ReturnStepId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.NextStep).WithMany().HasForeignKey(x => x.NextStepId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.BypassToStep).WithMany().HasForeignKey(x => x.BypassToStepId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<ApprovalDetail>(entity =>
        {
            entity.HasOne(x => x.NextStep).WithMany().HasForeignKey(x => x.NextStepId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ReturnToStep).WithMany().HasForeignKey(x => x.ReturnToStepId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.BypassFromStep).WithMany().HasForeignKey(x => x.BypassFromStepId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.BypassToStep).WithMany().HasForeignKey(x => x.BypassToStepId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ApprovalRequest).WithMany(x => x.ApprovalDetails)
                .HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApproverInfo>()
            .HasOne(x => x.ApprovalDetail).WithMany(x => x.ApproverInfos)
            .HasForeignKey(x => x.ApprovalDetailId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RequestAttachment>(entity =>
        {
            entity.HasIndex(x => x.RequestId);
            entity.HasOne(x => x.Request).WithMany(x => x.Attachments)
                .HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApprovalHistory>(entity =>
        {
            entity.HasIndex(x => new { x.RequestId, x.ActionAt });
            entity.HasOne(x => x.Request).WithMany(x => x.Histories)
                .HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ApprovalDetail).WithMany()
                .HasForeignKey(x => x.ApprovalDetailId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<NotiToast>(entity =>
        {
            entity.HasOne<ApprovalRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.SendFor, x.IsRead });
            entity.HasIndex(x => x.CreatedAt);
        });

        builder.Entity<RequestBusinessData>(entity =>
        {
            entity.HasOne(x => x.Request).WithOne(x => x.BusinessData)
                .HasForeignKey<RequestBusinessData>(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(x => x.Duration).HasPrecision(10, 2);
            entity.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            entity.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.TemplateCode, x.StartAt });
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReplacementEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DriverEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Department>().WithMany().HasForeignKey(x => x.TargetDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BelongSite>().WithMany().HasForeignKey(x => x.WorkSiteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RequestMasterItem>().WithMany().HasForeignKey(x => x.BankId).OnDelete(DeleteBehavior.Restrict);
            foreach (var name in new[] { "LeaveTypeId", "PurposeId", "CountryId", "CityId", "PartnerId", "CurrencyId", "OvertimeTypeId", "PaymentMethodId", "ProposalCategoryId", "VehicleTypeId", "VehicleId" })
                entity.HasOne<RequestMasterItem>().WithMany().HasForeignKey(name).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RequestLineItem>(entity =>
        {
            entity.HasOne(x => x.BusinessData).WithMany(x => x.Items).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CatalogItem).WithMany().HasForeignKey(x => x.CatalogItemId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Quantity).HasPrecision(12, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.RequestId, x.LineOrder }).IsUnique();
        });
        builder.Entity<RequestParticipant>(entity =>
        {
            entity.HasKey(x => new { x.RequestId, x.EmployeeId });
            entity.HasOne(x => x.BusinessData).WithMany(x => x.Participants).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RequestMasterItem>(entity =>
        {
            entity.HasIndex(x => new { x.Category, x.Code }).IsUnique();
            entity.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
        RequestMasterSeed.Apply(builder);
        ConfigureOperations(builder);
        IdentityModelSeed.Apply(builder);
        ApprovalDemoSeed.Apply(builder);
        // Preserve Vietnamese text in legacy entities when targeting SQL Server.
        foreach (var entity in builder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties().Where(x => x.ClrType == typeof(string)))
            {
                var storeType = property.GetColumnType();
                if (storeType?.StartsWith("varchar", StringComparison.OrdinalIgnoreCase) == true)
                    property.SetColumnType("n" + storeType);
                property.SetIsUnicode(true);
            }
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<BelongSite> BelongSites => Set<BelongSite>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<ApprovalCase> ApprovalCases => Set<ApprovalCase>();
    public DbSet<ApprovalWorkflowVersion> ApprovalWorkflowVersions => Set<ApprovalWorkflowVersion>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalRole> ApprovalRoles => Set<ApprovalRole>();
    public DbSet<ApprovalDetail> ApprovalDetails => Set<ApprovalDetail>();
    public DbSet<ApproverInfo> ApproverInfos => Set<ApproverInfo>();
    public DbSet<FormApproverSetting> FormApprovers => Set<FormApproverSetting>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
    public DbSet<RequestBusinessData> RequestBusinessData => Set<RequestBusinessData>();
    public DbSet<RequestLineItem> RequestLineItems => Set<RequestLineItem>();
    public DbSet<RequestParticipant> RequestParticipants => Set<RequestParticipant>();
    public DbSet<RequestMasterItem> RequestMasterItems => Set<RequestMasterItem>();
    public DbSet<NotiToast> NotiToasts => Set<NotiToast>();
    public DbSet<InactiveSchedule> InactiveSchedules => Set<InactiveSchedule>();
    public DbSet<AccountStatusLog> AccountStatusLogs => Set<AccountStatusLog>();
    public DbSet<EmployeeWorkEvent> EmployeeWorkEvents => Set<EmployeeWorkEvent>();
    public DbSet<VehicleDispatch> VehicleDispatches => Set<VehicleDispatch>();
    public DbSet<DispatchHistory> DispatchHistories => Set<DispatchHistory>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();
    public DbSet<FleetVehicle> FleetVehicles => Set<FleetVehicle>();
    public DbSet<WorkCalendarPolicy> WorkCalendarPolicies => Set<WorkCalendarPolicy>();
    public DbSet<WorkCalendarException> WorkCalendarExceptions => Set<WorkCalendarException>();
    public DbSet<LeaveTypePolicy> LeaveTypePolicies => Set<LeaveTypePolicy>();
    public DbSet<LeaveAccount> LeaveAccounts => Set<LeaveAccount>();
    public DbSet<LeaveAdjustment> LeaveAdjustments => Set<LeaveAdjustment>();
    public DbSet<LeaveAllocation> LeaveAllocations => Set<LeaveAllocation>();

    private static void ConfigureOperations(ModelBuilder builder)
    {
        builder.Entity<EmployeeWorkEvent>(e => {
            e.HasIndex(x => new { x.RequestId, x.EmployeeId, x.Kind }).IsUnique();
            e.HasIndex(x => new { x.EmployeeId, x.StartAt, x.EndAt });
            e.HasOne<ApprovalRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<VehicleDispatch>(e => {
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.HasIndex(x => new { x.VehicleId, x.StartAt, x.EndAt });
            e.HasIndex(x => new { x.DriverEmployeeId, x.StartAt, x.EndAt });
            e.HasOne<ApprovalRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DriverEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BelongSite>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RequestMasterItem>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DispatchHistory>(e => {
            e.HasIndex(x => new { x.RequestId, x.At });
            e.HasOne<VehicleDispatch>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DriverProfile>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FleetVehicle>(e => {
            e.HasOne<RequestMasterItem>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BelongSite>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WorkCalendarPolicy>(e => {
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.HasOne<BelongSite>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WorkCalendarException>(e => {
            e.HasKey(x => new { x.SiteId, x.Date }); e.Property(x => x.Date).HasColumnType("date");
            e.HasOne<BelongSite>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LeaveTypePolicy>(e => {
            e.HasKey(x => new { x.SiteId, x.LeaveTypeId }); e.Property(x => x.Revision).IsConcurrencyToken();
            e.HasOne<BelongSite>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RequestMasterItem>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LeaveAccount>(e => {
            e.HasKey(x => new { x.EmployeeId, x.Year, x.LeaveTypeId }); e.Property(x => x.Revision).IsConcurrencyToken();
            e.Property(x => x.AnnualDays).HasPrecision(10, 2); e.Property(x => x.CarryOverDays).HasPrecision(10, 2);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RequestMasterItem>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LeaveAdjustment>(e => {
            e.Property(x => x.Days).HasPrecision(10, 2);
            e.HasOne<LeaveAccount>().WithMany().HasForeignKey(x => new { x.EmployeeId, x.Year, x.LeaveTypeId }).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LeaveAllocation>(e => {
            e.HasKey(x => new { x.RequestId, x.Year }); e.Property(x => x.Days).HasPrecision(10, 2);
            e.Property(x => x.PreviousDays).HasPrecision(10, 2);
            e.HasIndex(x => new { x.EmployeeId, x.Year, x.LeaveTypeId, x.Status });
            e.HasOne<ApprovalRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RequestMasterItem>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<AppRole>().HasData(
            new AppRole { Id = "a0000001-0000-4000-8000-000000000031", Name = RoleConstants.TransportDispatch, ConcurrencyStamp = "seed-transport-dispatch", NormalizedName = "TRANSPORT.DISPATCH", DisplayName = "Điều phối xe", CategoryName = "Vận hành sau phê duyệt", CategoryOrder = 3 },
            new AppRole { Id = "a0000001-0000-4000-8000-000000000032", Name = RoleConstants.HRLeaveView, ConcurrencyStamp = "seed-hr-leave-view", NormalizedName = "HR.LEAVE.VIEW", DisplayName = "Xem sổ phép", CategoryName = "Vận hành sau phê duyệt", CategoryOrder = 3 },
            new AppRole { Id = "a0000001-0000-4000-8000-000000000033", Name = RoleConstants.HRLeaveManage, ConcurrencyStamp = "seed-hr-leave-manage", NormalizedName = "HR.LEAVE.MANAGE", DisplayName = "Quản lý sổ phép", CategoryName = "Vận hành sau phê duyệt", CategoryOrder = 3 });
    }
}
