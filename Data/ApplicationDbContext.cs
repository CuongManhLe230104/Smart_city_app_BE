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
        }
    }
}