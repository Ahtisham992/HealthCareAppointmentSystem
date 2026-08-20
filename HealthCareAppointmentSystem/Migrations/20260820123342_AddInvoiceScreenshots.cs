using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceScreenshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentScreenshotUrl",
                table: "Invoices",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Invoices",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundScreenshotUrl",
                table: "Invoices",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentScreenshotUrl",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundScreenshotUrl",
                table: "Invoices");
        }
    }
}
