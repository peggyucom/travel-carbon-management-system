using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarterM.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVoidedColumnsFromRateAndEmissionConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarAllowanceHistories_AspNetUsers_VoidedById",
                table: "CarAllowanceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionFactors_AspNetUsers_VoidedById",
                table: "EmissionFactors");

            migrationBuilder.DropForeignKey(
                name: "FK_MealAllowanceHistories_AspNetUsers_VoidedById",
                table: "MealAllowanceHistories");

            migrationBuilder.DropIndex(
                name: "IX_MealAllowanceHistories_VoidedById",
                table: "MealAllowanceHistories");

            migrationBuilder.DropIndex(
                name: "IX_EmissionFactors_VoidedById",
                table: "EmissionFactors");

            migrationBuilder.DropIndex(
                name: "IX_CarAllowanceHistories_VoidedById",
                table: "CarAllowanceHistories");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "MealAllowanceHistories");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "MealAllowanceHistories");

            migrationBuilder.DropColumn(
                name: "VoidedById",
                table: "MealAllowanceHistories");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "EmissionFactors");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "EmissionFactors");

            migrationBuilder.DropColumn(
                name: "VoidedById",
                table: "EmissionFactors");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "CarAllowanceHistories");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "CarAllowanceHistories");

            migrationBuilder.DropColumn(
                name: "VoidedById",
                table: "CarAllowanceHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "MealAllowanceHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "MealAllowanceHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedById",
                table: "MealAllowanceHistories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "EmissionFactors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "EmissionFactors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedById",
                table: "EmissionFactors",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "CarAllowanceHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "CarAllowanceHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedById",
                table: "CarAllowanceHistories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsVoided", "VoidedAt", "VoidedById" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsVoided", "VoidedAt", "VoidedById" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsVoided", "VoidedAt", "VoidedById" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "EmissionFactors",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsVoided", "VoidedAt", "VoidedById" },
                values: new object[] { false, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_MealAllowanceHistories_VoidedById",
                table: "MealAllowanceHistories",
                column: "VoidedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmissionFactors_VoidedById",
                table: "EmissionFactors",
                column: "VoidedById");

            migrationBuilder.CreateIndex(
                name: "IX_CarAllowanceHistories_VoidedById",
                table: "CarAllowanceHistories",
                column: "VoidedById");

            migrationBuilder.AddForeignKey(
                name: "FK_CarAllowanceHistories_AspNetUsers_VoidedById",
                table: "CarAllowanceHistories",
                column: "VoidedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionFactors_AspNetUsers_VoidedById",
                table: "EmissionFactors",
                column: "VoidedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MealAllowanceHistories_AspNetUsers_VoidedById",
                table: "MealAllowanceHistories",
                column: "VoidedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
