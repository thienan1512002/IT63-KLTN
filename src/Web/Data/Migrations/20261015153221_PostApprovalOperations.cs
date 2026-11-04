using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PostApprovalOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OperationsSyncedAt",
                table: "ApprovalRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(1100)", maxLength: 1100, nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllDay = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkEvents_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkEvents_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FleetVehicles",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetVehicles", x => x.VehicleId);
                    table.ForeignKey(
                        name: "FK_FleetVehicles_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FleetVehicles_RequestMasterItems_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveAccounts",
                columns: table => new
                {
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    AnnualDays = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CarryOverDays = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveAccounts", x => new { x.EmployeeId, x.Year, x.LeaveTypeId });
                    table.ForeignKey(
                        name: "FK_LeaveAccounts_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveAccounts_RequestMasterItems_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveAllocations",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Days = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PolicyConfigured = table.Column<bool>(type: "bit", nullable: false),
                    DeductsBalance = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviousDays = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    ClassifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClassifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClassificationNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveAllocations", x => new { x.RequestId, x.Year });
                    table.ForeignKey(
                        name: "FK_LeaveAllocations_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveAllocations_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveAllocations_RequestMasterItems_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypePolicies",
                columns: table => new
                {
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    DeductsBalance = table.Column<bool>(type: "bit", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypePolicies", x => new { x.SiteId, x.LeaveTypeId });
                    table.ForeignKey(
                        name: "FK_LeaveTypePolicies_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveTypePolicies_RequestMasterItems_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDispatches",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    DriverEmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActualStartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDispatches", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_VehicleDispatches_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleDispatches_AspNetUsers_DriverEmployeeId",
                        column: x => x.DriverEmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleDispatches_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleDispatches_RequestMasterItems_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendarExceptions",
                columns: table => new
                {
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendarExceptions", x => new { x.SiteId, x.Date });
                    table.ForeignKey(
                        name: "FK_WorkCalendarExceptions_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendarPolicies",
                columns: table => new
                {
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    WorkingDaysMask = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendarPolicies", x => x.SiteId);
                    table.ForeignKey(
                        name: "FK_WorkCalendarPolicies_BelongSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "BelongSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Days = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveAdjustments_LeaveAccounts_EmployeeId_Year_LeaveTypeId",
                        columns: x => new { x.EmployeeId, x.Year, x.LeaveTypeId },
                        principalTable: "LeaveAccounts",
                        principalColumns: new[] { "EmployeeId", "Year", "LeaveTypeId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    DriverEmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchHistories_VehicleDispatches_RequestId",
                        column: x => x.RequestId,
                        principalTable: "VehicleDispatches",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "CategoryName", "CategoryOrder", "ConcurrencyStamp", "DisplayName", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "a0000001-0000-4000-8000-000000000031", "Vận hành sau phê duyệt", 3, "seed-transport-dispatch", "Điều phối xe", "Transport.Dispatch", "TRANSPORT.DISPATCH" },
                    { "a0000001-0000-4000-8000-000000000032", "Vận hành sau phê duyệt", 3, "seed-hr-leave-view", "Xem sổ phép", "HR.Leave.View", "HR.LEAVE.VIEW" },
                    { "a0000001-0000-4000-8000-000000000033", "Vận hành sau phê duyệt", 3, "seed-hr-leave-manage", "Quản lý sổ phép", "HR.Leave.Manage", "HR.LEAVE.MANAGE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchHistories_RequestId_At",
                table: "DispatchHistories",
                columns: new[] { "RequestId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkEvents_EmployeeId_StartAt_EndAt",
                table: "EmployeeWorkEvents",
                columns: new[] { "EmployeeId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkEvents_RequestId_EmployeeId_Kind",
                table: "EmployeeWorkEvents",
                columns: new[] { "RequestId", "EmployeeId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetVehicles_SiteId",
                table: "FleetVehicles",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAccounts_LeaveTypeId",
                table: "LeaveAccounts",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAdjustments_EmployeeId_Year_LeaveTypeId",
                table: "LeaveAdjustments",
                columns: new[] { "EmployeeId", "Year", "LeaveTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAllocations_EmployeeId_Year_LeaveTypeId_Status",
                table: "LeaveAllocations",
                columns: new[] { "EmployeeId", "Year", "LeaveTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAllocations_LeaveTypeId",
                table: "LeaveAllocations",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypePolicies_LeaveTypeId",
                table: "LeaveTypePolicies",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDispatches_DriverEmployeeId_StartAt_EndAt",
                table: "VehicleDispatches",
                columns: new[] { "DriverEmployeeId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDispatches_SiteId",
                table: "VehicleDispatches",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDispatches_VehicleId_StartAt_EndAt",
                table: "VehicleDispatches",
                columns: new[] { "VehicleId", "StartAt", "EndAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchHistories");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "EmployeeWorkEvents");

            migrationBuilder.DropTable(
                name: "FleetVehicles");

            migrationBuilder.DropTable(
                name: "LeaveAdjustments");

            migrationBuilder.DropTable(
                name: "LeaveAllocations");

            migrationBuilder.DropTable(
                name: "LeaveTypePolicies");

            migrationBuilder.DropTable(
                name: "WorkCalendarExceptions");

            migrationBuilder.DropTable(
                name: "WorkCalendarPolicies");

            migrationBuilder.DropTable(
                name: "VehicleDispatches");

            migrationBuilder.DropTable(
                name: "LeaveAccounts");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a0000001-0000-4000-8000-000000000031");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a0000001-0000-4000-8000-000000000032");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a0000001-0000-4000-8000-000000000033");

            migrationBuilder.DropColumn(
                name: "OperationsSyncedAt",
                table: "ApprovalRequests");
        }
    }
}
