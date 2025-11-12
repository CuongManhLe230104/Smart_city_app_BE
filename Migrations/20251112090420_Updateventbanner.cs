using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smartcity_BE.Migrations
{
    /// <inheritdoc />
    public partial class Updateventbanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EventBanners",
                columns: new[] { "Id", "Description", "ImageUrl", "Title" },
                values: new object[] { 3, "Từ 10h-22h hàng ngày, Công viên Bãi Trước", "https://topbariavungtauaz.com/wp-content/uploads/2023/09/le-hoi-am-thuc-vung-tau_3.jpg", "Hội chợ ẩm thực đường phố" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventBanners",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
