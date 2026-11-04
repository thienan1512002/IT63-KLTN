using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniversityCampusBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename only the original demo campus; retain any administrator customization.
            migrationBuilder.Sql("""
                UPDATE [BelongSites]
                SET [Name] = N'Cơ sở điều hành', [Code] = N'CS-DH',
                    [Description] = CASE WHEN [Description] = N'YP Factory'
                        THEN N'Cơ sở điều hành' ELSE [Description] END
                WHERE [Id] = 1 AND [Name] = N'YP Factory' AND [Code] = N'YPF';

                UPDATE [RequestTypes] SET [Name] = N'Đăng ký xe nhà trường'
                WHERE [TemplateCode] = N'VEHICLE' AND [Name] = N'Đặt xe công ty';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [BelongSites]
                SET [Name] = N'YP Factory', [Code] = N'YPF',
                    [Description] = CASE WHEN [Description] = N'Cơ sở điều hành'
                        THEN N'YP Factory' ELSE [Description] END
                WHERE [Id] = 1 AND [Name] = N'Cơ sở điều hành' AND [Code] = N'CS-DH';

                UPDATE [RequestTypes] SET [Name] = N'Đặt xe công ty'
                WHERE [TemplateCode] = N'VEHICLE' AND [Name] = N'Đăng ký xe nhà trường';
                """);
        }
    }
}
