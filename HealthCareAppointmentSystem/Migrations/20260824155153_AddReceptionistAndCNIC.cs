using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCareAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionistAndCNIC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CNIC",
                table: "Patients",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CollectedByReceptionistId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Receptionists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CashDrawerBalance = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receptionists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receptionists_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceptionistId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    HandoverDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CollectedByReceptionistId",
                table: "Invoices",
                column: "CollectedByReceptionistId");

            migrationBuilder.CreateIndex(
                name: "IX_CashHandovers_AdminUserId",
                table: "CashHandovers",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashHandovers_ReceptionistId",
                table: "CashHandovers",
                column: "ReceptionistId");

            migrationBuilder.CreateIndex(
                name: "IX_Receptionists_ApplicationUserId",
                table: "Receptionists",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Receptionists_CollectedByReceptionistId",
                table: "Invoices",
                column: "CollectedByReceptionistId",
                principalTable: "Receptionists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Receptionists_CollectedByReceptionistId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "CashHandovers");

            migrationBuilder.DropTable(
                name: "Receptionists");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CollectedByReceptionistId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CNIC",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CollectedByReceptionistId",
                table: "Invoices");
        }
    }
}
