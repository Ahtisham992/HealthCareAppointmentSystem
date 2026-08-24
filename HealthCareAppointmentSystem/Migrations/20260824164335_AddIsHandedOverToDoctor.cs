using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsHandedOverToDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHandedOverToDoctor",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHandedOverToDoctor",
                table: "Invoices");
        }
    }
}
