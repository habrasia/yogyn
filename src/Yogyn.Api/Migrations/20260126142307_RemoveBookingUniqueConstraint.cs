using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yogyn.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBookingUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Booking_Unique",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StudioId",
                table: "Bookings",
                column: "StudioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_StudioId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_Unique",
                table: "Bookings",
                columns: new[] { "StudioId", "SessionId", "Email" },
                unique: true);
        }
    }
}
