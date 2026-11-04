using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequestBusinessForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "RequestTypes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "INTERNAL");

            migrationBuilder.CreateTable(
                name: "RequestMasterItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestMasterItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestMasterItems_RequestMasterItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestBusinessData",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReplacementEmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetDepartmentId = table.Column<int>(type: "int", nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: true),
                    StartSession = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EndSession = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PurposeId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OvertimeTypeId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: true),
                    Payee = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProposalCategoryId = table.Column<int>(type: "int", nullable: true),
                    ExpectedBenefit = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    DriverEmployeeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PickupLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PassengerCount = table.Column<int>(type: "int", nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestBusinessData", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_ApprovalRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_AspNetUsers_DriverEmployeeId",
                        column: x => x.DriverEmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_AspNetUsers_ReplacementEmployeeId",
                        column: x => x.ReplacementEmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_Departments_TargetDepartmentId",
                        column: x => x.TargetDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_CityId",
                        column: x => x.CityId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_CountryId",
                        column: x => x.CountryId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_OvertimeTypeId",
                        column: x => x.OvertimeTypeId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_ProposalCategoryId",
                        column: x => x.ProposalCategoryId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_PurposeId",
                        column: x => x.PurposeId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestBusinessData_RequestMasterItems_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    LineOrder = table.Column<int>(type: "int", nullable: false),
                    CatalogItemId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestLineItems_RequestBusinessData_RequestId",
                        column: x => x.RequestId,
                        principalTable: "RequestBusinessData",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestLineItems_RequestMasterItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "RequestMasterItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestParticipants",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestParticipants", x => new { x.RequestId, x.EmployeeId });
                    table.ForeignKey(
                        name: "FK_RequestParticipants_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestParticipants_RequestBusinessData_RequestId",
                        column: x => x.RequestId,
                        principalTable: "RequestBusinessData",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RequestMasterItems",
                columns: new[] { "Id", "Capacity", "Category", "Code", "IsActive", "Name", "ParentId", "Unit" },
                values: new object[,]
                {
                    { 1, null, "LeaveType", "ANNUAL", true, "Nghỉ phép năm", null, null },
                    { 2, null, "LeaveType", "UNPAID", true, "Nghỉ không hưởng lương", null, null },
                    { 3, null, "LeaveType", "SICK", true, "Nghỉ ốm", null, null },
                    { 4, null, "LeaveType", "PERSONAL", true, "Nghỉ việc riêng", null, null },
                    { 10, null, "TripPurpose", "CUSTOMER", true, "Gặp gỡ khách hàng / Đối tác", null, null },
                    { 11, null, "TripPurpose", "CONFERENCE", true, "Tham gia hội thảo / Triển lãm", null, null },
                    { 12, null, "TripPurpose", "TRAINING", true, "Đào tạo / Tập huấn", null, null },
                    { 13, null, "TripPurpose", "SITE_VISIT", true, "Khảo sát / Làm việc tại cơ sở", null, null },
                    { 20, null, "Country", "VN", true, "Việt Nam", null, null },
                    { 21, null, "Country", "JP", true, "Nhật Bản", null, null },
                    { 22, null, "Country", "SG", true, "Singapore", null, null },
                    { 40, null, "Currency", "VND", true, "Việt Nam đồng (VND)", null, null },
                    { 41, null, "Currency", "JPY", true, "Yên Nhật (JPY)", null, null },
                    { 42, null, "Currency", "USD", true, "Đô la Mỹ (USD)", null, null },
                    { 43, null, "Currency", "SGD", true, "Đô la Singapore (SGD)", null, null },
                    { 50, null, "ExpenseCategory", "AIRFARE", true, "Vé máy bay", null, "vé" },
                    { 51, null, "ExpenseCategory", "HOTEL", true, "Lưu trú / Khách sạn", null, "đêm" },
                    { 52, null, "ExpenseCategory", "MEALS", true, "Ăn uống / Công tác phí", null, "ngày" },
                    { 53, null, "ExpenseCategory", "TRANSPORT", true, "Di chuyển", null, "lượt" },
                    { 54, null, "ExpenseCategory", "SERVICE", true, "Dịch vụ", null, "lần" },
                    { 55, null, "ExpenseCategory", "OTHER", true, "Chi phí khác", null, "khoản" },
                    { 60, null, "Product", "LAPTOP", true, "Máy tính xách tay", null, "chiếc" },
                    { 61, null, "Product", "STATIONERY", true, "Văn phòng phẩm", null, "bộ" },
                    { 70, null, "Equipment", "LAPTOP", true, "Máy tính xách tay", null, "chiếc" },
                    { 71, null, "Equipment", "MONITOR", true, "Màn hình", null, "chiếc" },
                    { 72, null, "Equipment", "KEYBOARD", true, "Bàn phím", null, "chiếc" },
                    { 73, null, "Equipment", "MOUSE", true, "Chuột máy tính", null, "chiếc" },
                    { 80, null, "OvertimeType", "WEEKDAY", true, "Làm thêm ngày thường", null, null },
                    { 81, null, "OvertimeType", "WEEKEND", true, "Làm thêm ngày nghỉ", null, null },
                    { 82, null, "OvertimeType", "HOLIDAY", true, "Làm thêm ngày lễ", null, null },
                    { 90, null, "PaymentMethod", "TRANSFER", true, "Chuyển khoản", null, null },
                    { 91, null, "PaymentMethod", "CASH", true, "Tiền mặt", null, null },
                    { 100, null, "ProposalCategory", "PROCESS", true, "Cải tiến quy trình", null, null },
                    { 101, null, "ProposalCategory", "WORKPLACE", true, "Cải thiện môi trường làm việc", null, null },
                    { 102, null, "ProposalCategory", "TRAINING", true, "Đào tạo nhân sự", null, null },
                    { 103, null, "ProposalCategory", "OTHER", true, "Đề xuất khác", null, null },
                    { 110, 3, "VehicleType", "4_SEATS", true, "Xe 4 chỗ", null, null },
                    { 111, 6, "VehicleType", "7_SEATS", true, "Xe 7 chỗ", null, null },
                    { 112, 15, "VehicleType", "16_SEATS", true, "Xe 16 chỗ", null, null }
                });

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "TemplateCode",
                value: "LEAVE");

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "TemplateCode",
                value: "PURCHASE");

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "TemplateCode",
                value: "PAYMENT");

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "TemplateCode",
                value: "EQUIPMENT");

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "TemplateCode",
                value: "OVERTIME");

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "TemplateCode",
                value: "INTERNAL");

            migrationBuilder.InsertData(
                table: "RequestMasterItems",
                columns: new[] { "Id", "Capacity", "Category", "Code", "IsActive", "Name", "ParentId", "Unit" },
                values: new object[,]
                {
                    { 30, null, "City", "VN-HN", true, "Hà Nội", 20, null },
                    { 31, null, "City", "VN-HCM", true, "Thành phố Hồ Chí Minh", 20, null },
                    { 32, null, "City", "VN-DN", true, "Đà Nẵng", 20, null },
                    { 33, null, "City", "JP-TOKYO", true, "Tokyo", 21, null },
                    { 34, null, "City", "JP-OSAKA", true, "Osaka", 21, null },
                    { 35, null, "City", "SG-SINGAPORE", true, "Singapore", 22, null }
                });

            // Map existing type codes without changing historical request content.
            migrationBuilder.Sql("""
                UPDATE RequestTypes SET TemplateCode = CASE
                    WHEN UPPER(Code) IN ('LEAVE','PURCHASE','PAYMENT','EQUIPMENT','OVERTIME','INTERNAL') THEN UPPER(Code)
                    WHEN UPPER(Code) IN ('BUSINESS_TRIP','TRAVEL','BUS') THEN 'BUSINESS_TRIP'
                    WHEN UPPER(Code) IN ('VEHICLE','VEH') THEN 'VEHICLE'
                    ELSE TemplateCode END;
                INSERT INTO RequestTypes (Code, TemplateCode, Name, Description, IsActive, RequireRequestedDate, RequireAmount, RequireReason, AllowAttachment, ApprovalCaseId, CreatedAt)
                SELECT 'BUSINESS_TRIP', 'BUSINESS_TRIP', N'Đăng ký công tác', N'Đăng ký lịch trình và dự toán chi phí công tác', 1, 1, 0, 1, 1, CaseId, SYSUTCDATETIME()
                FROM ApprovalCases WHERE CaseId = 'WF-MULTI' AND IsActive = 1
                AND NOT EXISTS (SELECT 1 FROM RequestTypes WHERE TemplateCode = 'BUSINESS_TRIP');
                INSERT INTO RequestTypes (Code, TemplateCode, Name, Description, IsActive, RequireRequestedDate, RequireAmount, RequireReason, AllowAttachment, ApprovalCaseId, CreatedAt)
                SELECT 'VEHICLE', 'VEHICLE', N'Đặt xe công ty', N'Đăng ký lịch trình và phương tiện công tác', 1, 1, 0, 1, 1, CaseId, SYSUTCDATETIME()
                FROM ApprovalCases WHERE CaseId = 'WF-ONE' AND IsActive = 1
                AND NOT EXISTS (SELECT 1 FROM RequestTypes WHERE TemplateCode = 'VEHICLE');
                """);
            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_CityId",
                table: "RequestBusinessData",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_CountryId",
                table: "RequestBusinessData",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_CurrencyId",
                table: "RequestBusinessData",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_DriverEmployeeId",
                table: "RequestBusinessData",
                column: "DriverEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_EmployeeId",
                table: "RequestBusinessData",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_LeaveTypeId",
                table: "RequestBusinessData",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_OvertimeTypeId",
                table: "RequestBusinessData",
                column: "OvertimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_PartnerId",
                table: "RequestBusinessData",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_PaymentMethodId",
                table: "RequestBusinessData",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_ProposalCategoryId",
                table: "RequestBusinessData",
                column: "ProposalCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_PurposeId",
                table: "RequestBusinessData",
                column: "PurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_ReplacementEmployeeId",
                table: "RequestBusinessData",
                column: "ReplacementEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_TargetDepartmentId",
                table: "RequestBusinessData",
                column: "TargetDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_TemplateCode_StartAt",
                table: "RequestBusinessData",
                columns: new[] { "TemplateCode", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_VehicleId",
                table: "RequestBusinessData",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_VehicleTypeId",
                table: "RequestBusinessData",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLineItems_CatalogItemId",
                table: "RequestLineItems",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLineItems_RequestId_LineOrder",
                table: "RequestLineItems",
                columns: new[] { "RequestId", "LineOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestMasterItems_Category_Code",
                table: "RequestMasterItems",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestMasterItems_ParentId",
                table: "RequestMasterItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestParticipants_EmployeeId",
                table: "RequestParticipants",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestLineItems");

            migrationBuilder.DropTable(
                name: "RequestParticipants");

            migrationBuilder.DropTable(
                name: "RequestBusinessData");

            migrationBuilder.DropTable(
                name: "RequestMasterItems");

            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "RequestTypes");
        }
    }
}

