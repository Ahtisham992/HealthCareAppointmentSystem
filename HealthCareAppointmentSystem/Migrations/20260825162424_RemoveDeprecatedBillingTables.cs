using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeprecatedBillingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashHandovers");

            migrationBuilder.DropTable(
                name: "PlatformBills");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReceptionistId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    HandoverDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashHandovers_AspNetUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashHandovers_Receptionists_ReceptionistId",
                        column: x => x.ReceptionistId,
                        principalTable: "Receptionists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalEarnings = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformBills_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashHandovers_AdminUserId",
                table: "CashHandovers",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashHandovers_ReceptionistId",
                table: "CashHandovers",
                column: "ReceptionistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformBills_DoctorId",
                table: "PlatformBills",
                column: "DoctorId");
        }
    }
}
