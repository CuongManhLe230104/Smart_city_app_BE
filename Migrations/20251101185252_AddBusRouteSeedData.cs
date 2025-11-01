using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Smartcity_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddBusRouteSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusRoutes",
                columns: new[] { "Id", "RouteName", "RouteNumber", "Schedule" },
                values: new object[,]
                {
                    { 1, "Vũng Tàu – Phú Túc (Đồng Nai)", "22", "5:00 - 18:00" },
                    { 2, "Vũng Tàu – Phú Mỹ", "6", "5:30 - 18:30" },
                    { 3, "Vũng Tàu – Bình Châu", "4", "5:00 - 17:30" },
                    { 4, "Xuyên Mộc – Dầu Giây (Đồng Nai)", "15", "6:00 - 18:00" },
                    { 5, "Bình Châu – Bình Thuận", "8", "5:45 - 17:45" },
                    { 6, "Quốc Lộ 51 – Ngã Tư Vũng Tàu", "611 (cũ)", "5:00 - 18:00" },
                    { 7, "Vũng Tàu – Ngã Tư Vũng Tàu", "611 (mới)", "5:30 - 18:30" },
                    { 8, "Long Điền – Đồng Nai", "606", "5:15 - 18:15" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "BusRoutes",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
