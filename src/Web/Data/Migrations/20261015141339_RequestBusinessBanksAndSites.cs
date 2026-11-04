using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequestBusinessBanksAndSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "RequestBusinessData",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkSiteId",
                table: "RequestBusinessData",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_BankId",
                table: "RequestBusinessData",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestBusinessData_WorkSiteId",
                table: "RequestBusinessData",
                column: "WorkSiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestBusinessData_BelongSites_WorkSiteId",
                table: "RequestBusinessData",
                column: "WorkSiteId",
                principalTable: "BelongSites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestBusinessData_RequestMasterItems_BankId",
                table: "RequestBusinessData",
                column: "BankId",
                principalTable: "RequestMasterItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestBusinessData_BelongSites_WorkSiteId",
                table: "RequestBusinessData");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestBusinessData_RequestMasterItems_BankId",
                table: "RequestBusinessData");

            migrationBuilder.DropIndex(
                name: "IX_RequestBusinessData_BankId",
                table: "RequestBusinessData");

            migrationBuilder.DropIndex(
                name: "IX_RequestBusinessData_WorkSiteId",
                table: "RequestBusinessData");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "RequestBusinessData");

            migrationBuilder.DropColumn(
                name: "WorkSiteId",
                table: "RequestBusinessData");
        }
    }
}
