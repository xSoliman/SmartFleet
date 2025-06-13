using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFleet.Migrations
{
    /// <inheritdoc />
    public partial class f : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Speed",
                table: "VehicleLocations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: "2",
                column: "LicenseExpiryDate",
                value: new DateTime(2027, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(7838));

            migrationBuilder.UpdateData(
                table: "Maintenances",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(7930));

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "TripEndDate", "TripStartDate" },
                values: new object[] { new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(7968), new DateTime(2025, 6, 11, 3, 2, 47, 279, DateTimeKind.Local).AddTicks(7966), new DateTime(2025, 6, 11, 1, 2, 47, 279, DateTimeKind.Local).AddTicks(7963) });

            migrationBuilder.UpdateData(
                table: "SimCards",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedAt" },
                values: new object[] { new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(8009), new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(8012) });

            migrationBuilder.UpdateData(
                table: "Trips",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "StartTime" },
                values: new object[] { new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(8052), new DateTime(2025, 6, 11, 1, 2, 47, 279, DateTimeKind.Local).AddTicks(8049) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "19fb531c-00ac-4540-b20d-7805b171b346", new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(7566), "66c956ba-66d5-46c4-927a-fa0f34145237" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "97155c92-bcfe-4ca6-be49-e0c93d88d07d", new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(7848), "63a8c561-c828-4635-b6d7-8ce2d7b17cff" });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(7884));

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 11, 0, 2, 47, 279, DateTimeKind.Local).AddTicks(7890));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Speed",
                table: "VehicleLocations");

            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: "2",
                column: "LicenseExpiryDate",
                value: new DateTime(2027, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7319));

            migrationBuilder.UpdateData(
                table: "Maintenances",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7410));

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "TripEndDate", "TripStartDate" },
                values: new object[] { new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7452), new DateTime(2025, 2, 28, 8, 38, 54, 235, DateTimeKind.Local).AddTicks(7449), new DateTime(2025, 2, 28, 6, 38, 54, 235, DateTimeKind.Local).AddTicks(7444) });

            migrationBuilder.UpdateData(
                table: "SimCards",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedAt" },
                values: new object[] { new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7484), new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7488) });

            migrationBuilder.UpdateData(
                table: "Trips",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "StartTime" },
                values: new object[] { new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7528), new DateTime(2025, 2, 28, 6, 38, 54, 235, DateTimeKind.Local).AddTicks(7523) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "2a1d1cb9-0b78-4bc9-b095-01bb3fc9c43f", new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7080), "1c09bd9c-2d60-47bd-b146-7ce08086e954" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "78035726-b052-4e83-b87f-aaa63ea7e3d4", new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7325), "ed68c8a0-b1d6-4649-a9dd-ce02f715d2a4" });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7365));

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 2, 28, 5, 38, 54, 235, DateTimeKind.Local).AddTicks(7373));
        }
    }
}
