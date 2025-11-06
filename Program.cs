using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Đọc chuỗi kết nối từ appsettings.json ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 2. Đăng ký DbContext (file ApplicationDbContext của bạn) ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- 3. Đăng ký dịch vụ Controllers để chạy API ---
builder.Services.AddControllers();

// 4. THÊM DỊCH VỤ HTTPCLIENT (SỬA LỖI 500)
// (Dòng này bị thiếu, rất quan trọng để Controller gọi API LocationIQ)
builder.Services.AddHttpClient();

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

// app.UseHttpsRedirection(); // Tắt Https để test local dễ hơn

// Báo cho app sử dụng các Controllers (API)
app.MapControllers();

app.Run();