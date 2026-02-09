using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yogyn.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingApprovalFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoApproveReturning",
                table: "Studios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Studios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoApproveReturning",
                table: "Studios");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "Studios");
        }
    }
}
