using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Prescriptions",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "PrescriptionItems",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "PrescriptionItems");
        }
    }
}
