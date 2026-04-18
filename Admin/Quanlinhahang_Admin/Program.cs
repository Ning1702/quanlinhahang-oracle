using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Admin.Services;

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string từ appsettings hoặc Render env:
// ConnectionStrings__DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ====================== CẤU HÌNH DỊCH VỤ ======================
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseNpgsql(connectionString));

// Đăng ký service upload/xóa ảnh cho quản lý món ăn
builder.Services.AddScoped<IStorageService, StorageService>();

// Background service kiểm tra đơn quá hạn
builder.Services.AddHostedService<BookingTimeoutService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Giữ tạm kiểu này để app chạy ổn trên Render.
// Lưu ý: deploy/restart có thể đăng xuất phiên cũ.
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();

builder.Services.AddHttpContextAccessor();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ====================== BUILD APP ======================
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự chuẩn
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ====================== ROUTE ======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Render port binding
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();