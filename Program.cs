// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartCity_BE.Data;
using SmartCity_BE.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Kestrel (LAN Access)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<NotificationService>();

// ✅ THÊM: JWT Authentication Configuration
var jwtKey = builder.Configuration["JWT:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT:Key is not configured in appsettings.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Logging để debug
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ JWT Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            Console.WriteLine($"✅ JWT Token validated - UserId: {userId}, Email: {email}");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"📥 JWT Token received: {token.Substring(0, Math.Min(30, token.Length))}...");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 2. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- PHẦN BUILD APP ---
var app = builder.Build();

app.UseStaticFiles();

// 3. Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Logging Middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"🔍 {context.Request.Method} {context.Request.Path} from {context.Request.Headers.UserAgent}");
    await next();
});

// 4. CORS (Đặt trước Authentication)
app.UseCors("AllowReactApp");

// 5. ✅ Authentication & Authorization (Thứ tự quan trọng)
app.UseAuthentication();
app.UseAuthorization();

// 6. Map Controllers
app.MapControllers();

app.Run();