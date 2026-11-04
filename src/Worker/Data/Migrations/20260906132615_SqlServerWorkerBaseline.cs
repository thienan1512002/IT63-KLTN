using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class SqlServerWorkerBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "worker");

            migrationBuilder.CreateTable(
                name: "EmailMasterDatas",
                schema: "worker",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkflowAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CcAddresses = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TemplateDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    RetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMasterDatas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                schema: "worker",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailMasterDataId = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLogs_EmailMasterDatas_EmailMasterDataId",
                        column: x => x.EmailMasterDataId,
                        principalSchema: "worker",
                        principalTable: "EmailMasterDatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_CreatedAt",
                schema: "worker",
                table: "EmailLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_EmailMasterDataId",
                schema: "worker",
                table: "EmailLogs",
                column: "EmailMasterDataId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMasterDatas_CreatedAt",
                schema: "worker",
                table: "EmailMasterDatas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMasterDatas_Status",
                schema: "worker",
                table: "EmailMasterDatas",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs",
                schema: "worker");

            migrationBuilder.DropTable(
                name: "EmailMasterDatas",
                schema: "worker");
        }
    }
}
