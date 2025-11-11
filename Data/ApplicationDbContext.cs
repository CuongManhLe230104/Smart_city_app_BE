using Microsoft.EntityFrameworkCore;
using Smartcity_BE.Models;

namespace SmartCity_BE.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo các bảng
        // public DbSet<NguoiDung> NguoiDung { get; set; } = default!;
        // public DbSet<PhanAnh> PhanAnh { get; set; } = default!;
        public DbSet<BusRoute> BusRoutes { get; set; } = default!;
        public DbSet<EventBanner> EventBanners { get; set; } = default!;

        // --- 2. THÊM HÀM OnModelCreating BỊ THIẾU ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Thêm dữ liệu mẫu cho 8 tuyến xe buýt
            modelBuilder.Entity<BusRoute>().HasData(
                new BusRoute { Id = 1, RouteNumber = "22", RouteName = "Vũng Tàu – Phú Túc (Đồng Nai)", Schedule = "5:00 - 18:00" },
                new BusRoute { Id = 2, RouteNumber = "6", RouteName = "Vũng Tàu – Phú Mỹ", Schedule = "5:30 - 18:30" },
                new BusRoute { Id = 3, RouteNumber = "4", RouteName = "Vũng Tàu – Bình Châu", Schedule = "5:00 - 17:30" },
                new BusRoute { Id = 4, RouteNumber = "15", RouteName = "Xuyên Mộc – Dầu Giây (Đồng Nai)", Schedule = "6:00 - 18:00" },
                new BusRoute { Id = 5, RouteNumber = "8", RouteName = "Bình Châu – Bình Thuận", Schedule = "5:45 - 17:45" },
                new BusRoute { Id = 6, RouteNumber = "611 (cũ)", RouteName = "Quốc Lộ 51 – Ngã Tư Vũng Tàu", Schedule = "5:00 - 18:00" },
                new BusRoute { Id = 7, RouteNumber = "611 (mới)", RouteName = "Vũng Tàu – Ngã Tư Vũng Tàu", Schedule = "5:30 - 18:30" },
                new BusRoute { Id = 8, RouteNumber = "606", RouteName = "Long Điền – Đồng Nai", Schedule = "5:15 - 18:15" }
            );

            modelBuilder.Entity<EventBanner>().HasData(
                new EventBanner
                {
                    Id = 1,
                    Title = "Chợ quê Kim Bồng",
                    Description = "15h-22h 11/10/2025 (20 tháng 8 Âm Tịch)",
                    // Thay thế bằng link ảnh thật của bạn
                    ImageUrl = "https://happytour.com.vn/public/userfiles/tour/63/449835692_1035433271918761_6419380721802763959_n.jpg"
                },
                new EventBanner
                {
                    Id = 2,
                    Title = "Lễ hội Âm nhạc Bãi Sau",
                    Description = "Cuối tuần này, 20:00, Bãi Sau",
                    ImageUrl = "https://ongvove.com/uploads/0000/17/2024/05/07/le-hoi-am-nhac-bien-vung-tau-la-gi.jpg"
                }
            );
        }
    }
}