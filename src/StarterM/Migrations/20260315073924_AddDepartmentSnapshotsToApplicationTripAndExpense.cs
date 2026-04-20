using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarterM.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentSnapshotsToApplicationTripAndExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ExpenseRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "DailyTrips",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_DepartmentId",
                table: "ExpenseRecords",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyTrips_DepartmentId",
                table: "DailyTrips",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_DepartmentId",
                table: "Applications",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Departments_DepartmentId",
                table: "Applications",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyTrips_Departments_DepartmentId",
                table: "DailyTrips",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseRecords_Departments_DepartmentId",
                table: "ExpenseRecords",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Departments_DepartmentId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyTrips_Departments_DepartmentId",
                table: "DailyTrips");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseRecords_Departments_DepartmentId",
                table: "ExpenseRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseRecords_DepartmentId",
                table: "ExpenseRecords");

            migrationBuilder.DropIndex(
                name: "IX_DailyTrips_DepartmentId",
                table: "DailyTrips");

            migrationBuilder.DropIndex(
                name: "IX_Applications_DepartmentId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ExpenseRecords");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "DailyTrips");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Applications");
        }
    }
}
