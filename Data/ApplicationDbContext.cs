using Microsoft.EntityFrameworkCore;
//using SmartCity_BE.Models; // <-- 1. Thêm using cho Models

namespace SmartCity_BE.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo các bảng
        public DbSet<NguoiDung> NguoiDung { get; set; } = default!;
        public DbSet<PhanAnh> PhanAnh { get; set; } = default!;
        public DbSet<BusRoute> BusRoutes { get; set; } = default!;

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
        }
    }
}