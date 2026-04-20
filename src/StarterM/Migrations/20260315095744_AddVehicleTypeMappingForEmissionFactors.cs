using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StarterM.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTypeMappingForEmissionFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmissionFactors_ExpenseItems_ExpenseItemId",
                table: "EmissionFactors");

            migrationBuilder.DropIndex(
                name: "IX_EmissionFactors_ExpenseItemId",
                table: "EmissionFactors");

            migrationBuilder.DropColumn(
                name: "ExpenseItemId",
                table: "EmissionFactors");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "EmissionFactors");

            migrationBuilder.AddColumn<int>(
                name: "VehicleTypeId",
                table: "EmissionFactors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VehicleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseItemVehicleTypeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseItemId = table.Column<int>(type: "int", nullable: false),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseItemVehicleTypeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseItemVehicleTypeMappings_ExpenseItems_ExpenseItemId",
                        column: x => x.ExpenseItemId,
                        principalTable: "ExpenseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseItemVehicleTypeMappings_VehicleTypes_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 1,
                column: "VehicleTypeId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 2,
                column: "VehicleTypeId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 3,
                column: "VehicleTypeId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 4,
                column: "VehicleTypeId",
                value: 4);

            migrationBuilder.InsertData(
                table: "VehicleTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "PersonalCar", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "自用車", 1 },
                    { 2, "HSR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "高鐵", 2 },
                    { 3, "Train", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "火車", 3 },
                    { 4, "Taxi", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "計程車", 4 }
                });

            migrationBuilder.InsertData(
                table: "ExpenseItemVehicleTypeMappings",
                columns: new[] { "Id", "CreatedAt", "ExpenseItemId", "VehicleTypeId" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1 },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2 },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 3 },
                    { 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmissionFactors_VehicleTypeId_EffectiveFrom",
                table: "EmissionFactors",
                columns: new[] { "VehicleTypeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseItemVehicleTypeMappings_ExpenseItemId",
                table: "ExpenseItemVehicleTypeMappings",
                column: "ExpenseItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseItemVehicleTypeMappings_VehicleTypeId",
                table: "ExpenseItemVehicleTypeMappings",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_Code",
                table: "VehicleTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionFactors_VehicleTypes_VehicleTypeId",
                table: "EmissionFactors",
                column: "VehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmissionFactors_VehicleTypes_VehicleTypeId",
                table: "EmissionFactors");

            migrationBuilder.DropTable(
                name: "ExpenseItemVehicleTypeMappings");

            migrationBuilder.DropTable(
                name: "VehicleTypes");

            migrationBuilder.DropIndex(
                name: "IX_EmissionFactors_VehicleTypeId_EffectiveFrom",
                table: "EmissionFactors");

            migrationBuilder.DropColumn(
                name: "VehicleTypeId",
                table: "EmissionFactors");

            migrationBuilder.AddColumn<int>(
                name: "ExpenseItemId",
                table: "EmissionFactors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "EmissionFactors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ExpenseItemId", "VehicleType" },
                values: new object[] { 1, "自用車" });

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ExpenseItemId", "VehicleType" },
                values: new object[] { 2, "高鐵" });

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ExpenseItemId", "VehicleType" },
                values: new object[] { 3, "火車" });

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ExpenseItemId", "VehicleType" },
                values: new object[] { 4, "計程車" });

            migrationBuilder.CreateIndex(
                name: "IX_EmissionFactors_ExpenseItemId",
                table: "EmissionFactors",
                column: "ExpenseItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionFactors_ExpenseItems_ExpenseItemId",
                table: "EmissionFactors",
                column: "ExpenseItemId",
                principalTable: "ExpenseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
