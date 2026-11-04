using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalWorkflowVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApprovalSteps_ApprovalCaseId",
                table: "ApprovalSteps");

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "ApprovalSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRevision",
                table: "ApprovalRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "ApprovalCases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowVersions",
                columns: table => new
                {
                    ApprovalCaseId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowVersions", x => new { x.ApprovalCaseId, x.Revision });
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowVersions_ApprovalCases_ApprovalCaseId",
                        column: x => x.ApprovalCaseId,
                        principalTable: "ApprovalCases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ApprovalCases",
                keyColumn: "CaseId",
                keyValue: "WF-MULTI",
                column: "Revision",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ApprovalCases",
                keyColumn: "CaseId",
                keyValue: "WF-ONE",
                column: "Revision",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: "STEP-MULTI-1",
                column: "Revision",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: "STEP-MULTI-2",
                column: "Revision",
                value: 1);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: "STEP-ONE-1",
                column: "Revision",
                value: 1);

            // Only current template steps form the initial published version. Previously
            // removed templates and all existing ApprovalDetails remain untouched.
            migrationBuilder.Sql(@"
                UPDATE [ApprovalCases] SET [Revision] = 1;
                UPDATE [ApprovalSteps] SET [Revision] = CASE WHEN [IsRemoved] = 0 THEN 1 ELSE 0 END;
                INSERT INTO [ApprovalWorkflowVersions] ([ApprovalCaseId], [Revision], [Name], [Description], [PublishedAt], [PublishedBy])
                SELECT [CaseId], 1, [CaseName], [Description], COALESCE([UpdatedAt], [CreatedAt]), COALESCE([UpdatedBy], [CreatedBy])
                FROM [ApprovalCases];
                UPDATE r SET [WorkflowRevision] = 1
                FROM [ApprovalRequests] r
                INNER JOIN [ApprovalCases] c ON c.[CaseId] = r.[ApprovalCaseId]
                WHERE r.[Status] = 5 OR NOT EXISTS (
                    SELECT 1 FROM [ApprovalDetails] d
                    INNER JOIN [ApprovalSteps] s ON s.[Id] = d.[ApprovalStepId]
                    WHERE d.[RequestId] = r.[Id] AND s.[IsRemoved] = 1
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalCaseId_Revision_StepOrder",
                table: "ApprovalSteps",
                columns: new[] { "ApprovalCaseId", "Revision", "StepOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalWorkflowVersions");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalSteps_ApprovalCaseId_Revision_StepOrder",
                table: "ApprovalSteps");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "ApprovalSteps");

            migrationBuilder.DropColumn(
                name: "WorkflowRevision",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "ApprovalCases");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalCaseId",
                table: "ApprovalSteps",
                column: "ApprovalCaseId");
        }
    }
}
