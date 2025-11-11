using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smartcity_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddEventBannersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhanAnh");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.UpdateData(
                table: "EventBanners",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "https://happytour.com.vn/public/userfiles/tour/63/449835692_1035433271918761_6419380721802763959_n.jpg");

            migrationBuilder.UpdateData(
                table: "EventBanners",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://ongvove.com/uploads/0000/17/2024/05/07/le-hoi-am-nhac-bien-vung-tau-la-gi.jpg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatKhau = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhanAnh",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiDungId = table.Column<long>(type: "bigint", nullable: false),
                    HinhAnhUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoiDung = table.Column<string>(type: "ntext", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanAnh", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhanAnh_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "EventBanners",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "https://i.imgur.com/g0P4fL5.jpeg");

            migrationBuilder.UpdateData(
                table: "EventBanners",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://i.imgur.com/Q9WqA8q.jpeg");

            migrationBuilder.CreateIndex(
                name: "IX_PhanAnh_NguoiDungId",
                table: "PhanAnh",
                column: "NguoiDungId");
        }
    }
}
