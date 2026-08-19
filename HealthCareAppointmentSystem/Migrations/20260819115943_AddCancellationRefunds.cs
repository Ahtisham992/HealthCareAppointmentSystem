using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRefunded",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRefunded",
                table: "Appointments");
        }
    }
}
