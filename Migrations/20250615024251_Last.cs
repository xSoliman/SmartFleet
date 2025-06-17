using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFleet.Migrations
{
    /// <inheritdoc />
    public partial class Last : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: "2",
                column: "LicenseExpiryDate",
                value: new DateTime(2027, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5582));

            migrationBuilder.UpdateData(
                table: "Maintenances",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5848));

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "TripEndDate", "TripStartDate" },
                values: new object[] { new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5916), new DateTime(2025, 6, 15, 8, 42, 50, 88, DateTimeKind.Local).AddTicks(5912), new DateTime(2025, 6, 15, 6, 42, 50, 88, DateTimeKind.Local).AddTicks(5904) });

            migrationBuilder.UpdateData(
                table: "SimCards",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedAt" },
                values: new object[] { new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5969), new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5972) });

            migrationBuilder.UpdateData(
                table: "Trips",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "StartTime" },
                values: new object[] { new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(6050), new DateTime(2025, 6, 15, 6, 42, 50, 88, DateTimeKind.Local).AddTicks(6039) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "de658035-570c-4e43-9ab1-c32623402789", new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5273), "7d126621-63a3-4ba2-a7cd-d3cbdbacf885" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "c273201f-67aa-406c-b3cc-91abeb3fef50", new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5591), "d9222c66-98db-46ee-8028-adde8e2b8819" });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5775));

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 15, 5, 42, 50, 88, DateTimeKind.Local).AddTicks(5787));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: "2",
                column: "LicenseExpiryDate",
                value: new DateTime(2027, 6, 14, 13, 11, 3, 481, DateTimeKind.Local).AddTicks(9892));

            migrationBuilder.UpdateData(
                table: "Maintenances",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 14, 13, 11, 3, 482, DateTimeKind.Local).AddTicks(59));

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "TripEndDate", "TripStartDate" },
                values: new object[] { new DateTime(2025, 6, 14, 13, 11, 3, 482, DateTimeKind.Local).AddTicks(142), new DateTime(2025, 6, 14, 16, 11, 3, 482, DateTimeKind.Local).AddTicks(136), new DateTime(2025, 6, 14, 14, 11, 3, 482, DateTimeKind.Local).AddTicks(128) });

            migrationBuilder.UpdateData(
                table: "SimCards",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedAt" },
                values: new object[] { new DateTime(2025, 6, 14, 13, 11, 3, 482, DateTimeKind.Local).AddTicks(200), new DateTime(2025, 6, 14, 13, 11, 3, 482, DateTimeKind.Local).AddTicks(206) });

            migrationBuilder.UpdateData(
                table: "Trips",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "StartTime" },
                values: new object[] { new DateTime(2025, 6, 14, 13, 11, 3, 482, DateTimeKind.Local).AddTicks(276), new DateTime(2025, 6, 14, 14, 11, 3, 482, DateTimeKind.Local).AddTicks(267) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "54c58364-7f26-4375-a8a1-cd6efb59fb8c", new DateTime(2025, 6, 14, 13, 11, 3, 481, DateTimeKind.Local).AddTicks(9413), "7e6b238e-0f65-4b08-a8b6-7bda698e9cc9" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "1baa5def-5411-4d6e-b9af-1bd0fd00acdf", new DateTime(2025, 6, 14, 13, 11, 3, 481, DateTimeKind.Local).AddTicks(9904), "2964324d-c3d7-42d0-af06-b4f181e55056" });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 14, 13, 11, 3, 481, DateTimeKind.Local).AddTicks(9974));

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 14, 13, 11, 3, 481, DateTimeKind.Local).AddTicks(9990));
        }
    }
}
