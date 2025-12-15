// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Models;

namespace SmartCity_BE.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo các bảng
        public DbSet<BusRoute> BusRoutes { get; set; } = default!;
        public DbSet<EventBanner> EventBanners { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Feedback> Feedbacks { get; set; } = default!;
        public DbSet<FloodReport> FloodReports { get; set; } = default!;
        public DbSet<TravelTour> TravelTours { get; set; } = default!;
        public DbSet<Booking> Bookings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng BusRoute
            modelBuilder.Entity<BusRoute>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RouteNumber).IsRequired().HasMaxLength(10);
                entity.Property(e => e.RouteName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Schedule).HasMaxLength(50);
            });

            // Cấu hình bảng EventBanner
            modelBuilder.Entity<EventBanner>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
            });

            // Cấu hình bảng User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            });

            // Cấu hình bảng Feedback
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasDefaultValue("Pending");

                // Relationship với User
                entity.HasOne(f => f.User)
                      .WithMany()
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure FloodReport relationships
            modelBuilder.Entity<FloodReport>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Không xóa cascade

            // Cấu hình bảng TravelTour
            modelBuilder.Entity<TravelTour>(entity =>
            {
                entity.ToTable("TravelTours"); // Đảm bảo ánh xạ tên bảng đã khai báo trong Model
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NameTour).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TourType).IsRequired().HasMaxLength(100);

                // Cấu hình Foreign Key đến User
                entity.HasOne(t => t.User)
                      .WithMany()
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // Giả định không muốn xóa Tour khi User bị xóa
            });

            // 3. Cấu hình bảng Booking (Đặt Tour)
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.BookingId); // Giả định Booking có Key là BookingId
                entity.Property(e => e.Status).HasDefaultValue("Pending");

                // Relationship với User (Người đặt)
                entity.HasOne(b => b.User)
                      .WithMany()
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship với TravelTour
                entity.HasOne(b => b.Tour)
                      .WithMany() // Nếu bạn muốn thêm ICollection<Booking> vào Model TravelTour, bạn có thể thay đổi WithMany
                      .HasForeignKey(b => b.TourId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}