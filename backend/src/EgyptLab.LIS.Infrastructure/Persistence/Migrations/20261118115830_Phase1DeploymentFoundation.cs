using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgyptLab.LIS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1DeploymentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "users");
        }
    }
}
