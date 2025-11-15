// Program.cs
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
// JWT Service
builder.Services.AddScoped<JwtService>();

// CORS configuration chi tiết
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPostman", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetIsOriginAllowed(origin => true); // Cho phép tất cả origins
    });
});

var app = builder.Build();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Thêm middleware để log requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"🔍 {context.Request.Method} {context.Request.Path} from {context.Request.Headers.UserAgent}");
    await next();
});

app.UseCors("AllowPostman");
app.MapControllers();

app.Run();