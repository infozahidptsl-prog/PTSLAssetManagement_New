using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQrTokenSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "QrTokenCreatedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrTokenHash",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrTokenRevokedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QrTokenVersion",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrTokenCreatedAt",
                table: "Devices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrTokenHash",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrTokenRevokedAt",
                table: "Devices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QrTokenVersion",
                table: "Devices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrTokenCreatedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "QrTokenHash",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "QrTokenRevokedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "QrTokenVersion",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "QrTokenCreatedAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "QrTokenHash",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "QrTokenRevokedAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "QrTokenVersion",
                table: "Devices");
        }
    }
}
