using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data; // <-- 1. BỎ COMMENT DÒNG NÀY
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Đọc chuỗi kết nối từ appsettings.json ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 2. Đăng ký DbContext (file ApplicationDbContext của bạn) ---
// Lỗi ở đây (dòng 14 cũ) sẽ tự biến mất sau khi bạn sửa dòng 3
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- 3. Đăng ký dịch vụ Controllers để chạy API ---
builder.Services.AddControllers();

// (Thêm dịch vụ Swagger để test API trên trình duyệt)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cấu hình Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// Báo cho app sử dụng các Controllers (API)
app.MapControllers();

app.Run();
