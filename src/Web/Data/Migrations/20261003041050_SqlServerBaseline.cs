using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SqlServerBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountStatusLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountStatusLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalCases",
                columns: table => new
                {
                    CaseId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BelongSite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FormCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalCases", x => x.CaseId);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LocalizationKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BelongSite = table.Column<int>(type: "int", nullable: false),
                    FormCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoleType = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    FormApprovalId = table.Column<int>(type: "int", nullable: true),
                    IdentityRoleName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryOrder = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BelongSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 300, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BelongSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormApprovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserNames = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FormCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormApprovers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InactiveSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    From = table.Column<DateTime>(type: "datetime2", nullable: false),
                    To = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeactivateJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReactivateJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InactiveSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequireRequestedDate = table.Column<bool>(type: "bit", nullable: false),
                    RequireAmount = table.Column<bool>(type: "bit", nullable: false),
                    RequireReason = table.Column<bool>(type: "bit", nullable: false),
                    AllowAttachment = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalCaseId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestTypes_ApprovalCases_ApprovalCaseId",
                        column: x => x.ApprovalCaseId,
                        principalTable: "ApprovalCases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovalRoleId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    ApprovalCaseId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    ActionTypes = table.Column<int>(type: "int", nullable: false),
                    ApprovalType = table.Column<int>(type: "int", nullable: false),
                    ReturnStepId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    NextStepId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    SendSubmissionNoty = table.Column<bool>(type: "bit", nullable: false),
                    ProcessOnlineOffline = table.Column<bool>(type: "bit", nullable: false),
                    GroupTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CheckBypass = table.Column<bool>(type: "bit", nullable: false),
                    BypassToStepId = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    IsParentStep = table.Column<bool>(type: "bit", nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalCases_ApprovalCaseId",
                        column: x => x.ApprovalCaseId,
                        principalTable: "ApprovalCases",
                        principalColumn: "CaseId");
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalRoles_ApprovalRoleId",
                        column: x => x.ApprovalRoleId,
                        principalTable: "ApprovalRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalSteps_BypassToStepId",
                        column: x => x.BypassToStepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalSteps_NextStepId",
                        column: x => x.NextStepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalSteps_ReturnStepId",
                        column: x => x.ReturnStepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MainApprover = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BackupApprover = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EngName = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    MainApprover = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    BackupApprover = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    PIC = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    DeputyApprovers = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sections_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpId = table.Column<string>(type: "nvarchar(300)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IsOperator = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    UrgencyLevel = table.Column<bool>(type: "bit", nullable: false),
                    FaxCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    DeptName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DeptId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Process = table.Column<int>(type: "int", nullable: false),
                    TotalStep = table.Column<int>(type: "int", nullable: false),
                    MainApproval = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Complete = table.Column<bool>(type: "bit", nullable: false),
                    CurrentApprovalType = table.Column<int>(type: "int", nullable: false),
                    FormCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApprovalCaseId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    RequestTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ApprovalCases_ApprovalCaseId",
                        column: x => x.ApprovalCaseId,
                        principalTable: "ApprovalCases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_RequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalTable: "RequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalStepId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", maxLength: 300, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    IsRemove = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrentStep = table.Column<bool>(type: "bit", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    ProcessOnlineOffline = table.Column<bool>(type: "bit", nullable: false),
                    IsRequester = table.Column<bool>(type: "bit", nullable: false),
                    IsLeader = table.Column<bool>(type: "bit", nullable: false),
                    StepTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GroupTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ActionTypes = table.Column<int>(type: "int", nullable: false),
                    ApprovalType = table.Column<int>(type: "int", nullable: false),
                    ReturnToStepId = table.Column<int>(type: "int", nullable: true),
                    NextStepId = table.Column<int>(type: "int", nullable: true),
                    SendSubmissionNoty = table.Column<bool>(type: "bit", nullable: false),
                    CheckBypass = table.Column<bool>(type: "bit", nullable: false),
                    BypassFromStepId = table.Column<int>(type: "int", nullable: true),
                    BypassToStepId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalDetails_ApprovalDetails_BypassFromStepId",
                        column: x => x.BypassFromStepId,
                        principalTable: "ApprovalDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalDetails_ApprovalDetails_BypassToStepId",
                        column: x => x.BypassToStepId,
                        principalTable: "ApprovalDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalDetails_ApprovalDetails_NextStepId",
                        column: x => x.NextStepId,
                        principalTable: "ApprovalDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalDetails_ApprovalDetails_ReturnToStepId",
                        column: x => x.ReturnToStepId,
                        principalTable: "ApprovalDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalDetails_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalDetails_ApprovalSteps_ApprovalStepId",
                        column: x => x.ApprovalStepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotiToasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SendFor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContentKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContentArgsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NeedTranslate = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotiToasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotiToasts_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestAttachments_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ApprovalDetailId = table.Column<int>(type: "int", nullable: true),
                    RequestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalHistories_ApprovalDetails_ApprovalDetailId",
                        column: x => x.ApprovalDetailId,
                        principalTable: "ApprovalDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalHistories_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApproverInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalDetailId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    SectionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApproverInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApproverInfos_ApprovalDetails_ApprovalDetailId",
                        column: x => x.ApprovalDetailId,
                        principalTable: "ApprovalDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ApprovalCases",
                columns: new[] { "CaseId", "BelongSite", "CaseName", "CreatedAt", "CreatedBy", "Description", "FormCode", "IsActive", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { "WF-MULTI", "ALL", "Phê duyệt hai bước", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Phê duyệt hai bước", "WF-MULTI", true, null, "seed" },
                    { "WF-ONE", "ALL", "Phê duyệt một bước", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Phê duyệt một bước", "WF-ONE", true, null, "seed" }
                });

            migrationBuilder.InsertData(
                table: "ApprovalRoles",
                columns: new[] { "Id", "BelongSite", "CreatedAt", "CreatedBy", "DepartmentId", "FormApprovalId", "FormCode", "IdentityRoleName", "IsActive", "LocalizationKey", "RoleType", "SectionId", "Title", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { "ROLE-DEPT-MANAGER-MULTI", 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", null, null, "WF-MULTI", null, true, null, 4, null, "Trưởng phòng", null, "seed" },
                    { "ROLE-DEPT-MANAGER-ONE", 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", null, null, "WF-ONE", null, true, null, 4, null, "Trưởng phòng", null, "seed" },
                    { "ROLE-DIRECTOR", 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", null, 1, "WF-MULTI", null, true, null, 1, null, "Giám đốc", null, "seed" }
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "CategoryName", "CategoryOrder", "ConcurrencyStamp", "DisplayName", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "a0000001-0000-4000-8000-000000000001", "He thong", 0, "seed-superuser-stamp-001", "Super User", "SuperUser", "SUPERUSER" },
                    { "a0000001-0000-4000-8000-000000000011", "Quản lý nhân sự", 1, "seed-hr-view-stamp-001", "Xem thông tin nhân sự", "HR.Personnel.View", "HR.PERSONNEL.VIEW" },
                    { "a0000001-0000-4000-8000-000000000012", "Quản lý nhân sự", 1, "seed-hr-manage-stamp-001", "Quản lý thông tin nhân sự", "HR.Personnel.Manage", "HR.PERSONNEL.MANAGE" },
                    { "a0000001-0000-4000-8000-000000000021", "Phê duyệt", 1, "seed-employee", "Nhân viên", "Employee", "EMPLOYEE" },
                    { "a0000001-0000-4000-8000-000000000022", "Phê duyệt", 1, "seed-approver", "Người phê duyệt", "Approver", "APPROVER" }
                });

            migrationBuilder.InsertData(
                table: "BelongSites",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[] { 1, "YPF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "YP Factory", true, "YP Factory", null });

            migrationBuilder.InsertData(
                table: "FormApprovers",
                columns: new[] { "Id", "DisplayTitle", "FormCode", "IsRemoved", "Title", "UserNames" },
                values: new object[] { 1, "Giám đốc", "WF-MULTI", false, "Giám đốc", "director1" });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ActionTypes", "ApprovalCaseId", "ApprovalRoleId", "ApprovalType", "BypassToStepId", "CheckBypass", "GroupTitle", "IsParentStep", "IsRemoved", "NextStepId", "ProcessOnlineOffline", "ReturnStepId", "SendSubmissionNoty", "StepOrder" },
                values: new object[,]
                {
                    { "STEP-MULTI-2", 23, "WF-MULTI", "ROLE-DIRECTOR", 1, null, false, "", null, false, null, false, null, false, 2 },
                    { "STEP-ONE-1", 23, "WF-ONE", "ROLE-DEPT-MANAGER-ONE", 1, null, false, "", null, false, null, false, null, false, 1 }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "BackupApprover", "CreatedAt", "CreatedBy", "Description", "IsActive", "MainApprover", "Name", "ShortName", "SiteId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Công nghệ thông tin", true, "manager1", "Công nghệ thông tin", "IT", 1, null, null },
                    { 2, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "Tài chính", true, "director1", "Tài chính", "FIN", 1, null, null },
                    { 3, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", "BOD", true, "director1", "BOD", "BOD", 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "RequestTypes",
                columns: new[] { "Id", "AllowAttachment", "ApprovalCaseId", "Code", "CreatedAt", "Description", "IsActive", "Name", "RequireAmount", "RequireReason", "RequireRequestedDate", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, "WF-ONE", "LEAVE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Đơn xin nghỉ phép", true, "Đơn xin nghỉ phép", false, true, true, null },
                    { 2, true, "WF-MULTI", "PURCHASE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Đề nghị mua hàng", true, "Đề nghị mua hàng", true, true, true, null },
                    { 3, true, "WF-MULTI", "PAYMENT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Đề nghị thanh toán", true, "Đề nghị thanh toán", true, true, true, null },
                    { 4, true, "WF-ONE", "EQUIPMENT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yêu cầu cấp thiết bị", true, "Yêu cầu cấp thiết bị", false, true, true, null },
                    { 5, true, "WF-ONE", "OVERTIME", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yêu cầu làm thêm giờ", true, "Yêu cầu làm thêm giờ", false, true, true, null },
                    { 6, true, "WF-MULTI", "INTERNAL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Đề xuất nội bộ", true, "Đề xuất nội bộ", false, true, false, null }
                });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ActionTypes", "ApprovalCaseId", "ApprovalRoleId", "ApprovalType", "BypassToStepId", "CheckBypass", "GroupTitle", "IsParentStep", "IsRemoved", "NextStepId", "ProcessOnlineOffline", "ReturnStepId", "SendSubmissionNoty", "StepOrder" },
                values: new object[] { "STEP-MULTI-1", 23, "WF-MULTI", "ROLE-DEPT-MANAGER-MULTI", 1, null, false, "", null, false, "STEP-MULTI-2", false, null, false, 1 });

            migrationBuilder.InsertData(
                table: "Sections",
                columns: new[] { "Id", "BackupApprover", "CreatedAt", "CreatedBy", "DepartmentId", "DeputyApprovers", "EngName", "IsActive", "MainApprover", "Name", "Note", "PIC", "ShortName", "SiteId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", 1, "", "IT General", true, "manager1", "IT General", "", "", "IT General", 1, null, null },
                    { 2, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", 2, "", "Finance General", true, "director1", "Finance General", "", "", "Finance General", 1, null, null },
                    { 3, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", 3, "", "BOD Office", true, "director1", "BOD Office", "", "", "BOD Office", 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "Avatar", "ConcurrencyStamp", "CreatedAt", "DateOfBirth", "Email", "EmailConfirmed", "EmpId", "FullName", "HireDate", "IsActive", "IsOnline", "IsOperator", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "Position", "SectionId", "SecurityStamp", "SiteId", "TwoFactorEnabled", "UpdatedAt", "UserName" },
                values: new object[,]
                {
                    { "b0000001-0000-4000-8000-000000000001", 0, "", "seed-nmvadmin-concurrency-stamp", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "nmvadmin@demo.local", true, "ADMIN001", "System Administrator", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, false, true, null, "NMVADMIN@DEMO.LOCAL", "NMVADMIN", "AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg==", null, false, "", 1, "seed-nmvadmin-security-stamp", 1, false, null, "nmvadmin" },
                    { "b0000001-0000-4000-8000-000000000021", 0, "", "seed-concurrency-employee1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "employee1@demo.local", true, "EMP001", "Nguyễn Văn Nhân Viên", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, false, true, null, "EMPLOYEE1@DEMO.LOCAL", "EMPLOYEE1", "AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg==", null, false, "Demo", 1, "seed-security-employee1", 1, false, null, "employee1" },
                    { "b0000001-0000-4000-8000-000000000022", 0, "", "seed-concurrency-manager1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "manager1@demo.local", true, "MGR001", "Trần Thị Trưởng Phòng", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, false, true, null, "MANAGER1@DEMO.LOCAL", "MANAGER1", "AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg==", null, false, "Demo", 1, "seed-security-manager1", 1, false, null, "manager1" },
                    { "b0000001-0000-4000-8000-000000000023", 0, "", "seed-concurrency-director1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "director1@demo.local", true, "DIR001", "Lê Văn Giám Đốc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, false, true, null, "DIRECTOR1@DEMO.LOCAL", "DIRECTOR1", "AQAAAAIAAYagAAAAEMJ5vVKY+0P4eZWbaTWPo4Gpa9IUh/V49rbn+pkRWNt8+mmjnn3U6Mhr/brblygijg==", null, false, "Demo", 3, "seed-security-director1", 1, false, null, "director1" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "UserId" },
                values: new object[,]
                {
                    { -4, "IsFirstTimeLogin", "false", "b0000001-0000-4000-8000-000000000023" },
                    { -3, "IsFirstTimeLogin", "false", "b0000001-0000-4000-8000-000000000022" },
                    { -2, "IsFirstTimeLogin", "false", "b0000001-0000-4000-8000-000000000021" },
                    { -1, "IsFirstTimeLogin", "false", "b0000001-0000-4000-8000-000000000001" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "a0000001-0000-4000-8000-000000000001", "b0000001-0000-4000-8000-000000000001" },
                    { "a0000001-0000-4000-8000-000000000021", "b0000001-0000-4000-8000-000000000021" },
                    { "a0000001-0000-4000-8000-000000000021", "b0000001-0000-4000-8000-000000000022" },
                    { "a0000001-0000-4000-8000-000000000022", "b0000001-0000-4000-8000-000000000022" },
                    { "a0000001-0000-4000-8000-000000000022", "b0000001-0000-4000-8000-000000000023" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDetails_ApprovalStepId",
                table: "ApprovalDetails",
                column: "ApprovalStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDetails_BypassFromStepId",
                table: "ApprovalDetails",
                column: "BypassFromStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDetails_BypassToStepId",
                table: "ApprovalDetails",
                column: "BypassToStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDetails_NextStepId",
                table: "ApprovalDetails",
                column: "NextStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDetails_RequestId",
                table: "ApprovalDetails",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDetails_ReturnToStepId",
                table: "ApprovalDetails",
                column: "ReturnToStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalHistories_ApprovalDetailId",
                table: "ApprovalHistories",
                column: "ApprovalDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalHistories_RequestId_ActionAt",
                table: "ApprovalHistories",
                columns: new[] { "RequestId", "ActionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ApplicationNo",
                table: "ApprovalRequests",
                column: "ApplicationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ApprovalCaseId",
                table: "ApprovalRequests",
                column: "ApprovalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_CreatedByUserId_Status",
                table: "ApprovalRequests",
                columns: new[] { "CreatedByUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_DepartmentId_Status",
                table: "ApprovalRequests",
                columns: new[] { "DepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestTypeId",
                table: "ApprovalRequests",
                column: "RequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalCaseId",
                table: "ApprovalSteps",
                column: "ApprovalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalRoleId",
                table: "ApprovalSteps",
                column: "ApprovalRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_BypassToStepId",
                table: "ApprovalSteps",
                column: "BypassToStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_NextStepId",
                table: "ApprovalSteps",
                column: "NextStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ReturnStepId",
                table: "ApprovalSteps",
                column: "ReturnStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApproverInfos_ApprovalDetailId",
                table: "ApproverInfos",
                column: "ApprovalDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmpId",
                table: "AspNetUsers",
                column: "EmpId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SectionId",
                table: "AspNetUsers",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SiteId",
                table: "AspNetUsers",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BelongSites_Name",
                table: "BelongSites",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_SiteId_Name",
                table: "Departments",
                columns: new[] { "SiteId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotiToasts_CreatedAt",
                table: "NotiToasts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotiToasts_RequestId",
                table: "NotiToasts",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_NotiToasts_SendFor_IsRead",
                table: "NotiToasts",
                columns: new[] { "SendFor", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestAttachments_RequestId",
                table: "RequestAttachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_ApprovalCaseId",
                table: "RequestTypes",
                column: "ApprovalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_Code",
                table: "RequestTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sections_DepartmentId_Name",
                table: "Sections",
                columns: new[] { "DepartmentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sections_SiteId",
                table: "Sections",
                column: "SiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountStatusLogs");

            migrationBuilder.DropTable(
                name: "ApprovalHistories");

            migrationBuilder.DropTable(
                name: "ApproverInfos");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "FormApprovers");

            migrationBuilder.DropTable(
                name: "InactiveSchedules");

            migrationBuilder.DropTable(
                name: "NotiToasts");

            migrationBuilder.DropTable(
                name: "RequestAttachments");

            migrationBuilder.DropTable(
                name: "ApprovalDetails");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "ApprovalSteps");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "RequestTypes");

            migrationBuilder.DropTable(
                name: "ApprovalRoles");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "ApprovalCases");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "BelongSites");
        }
    }
}
