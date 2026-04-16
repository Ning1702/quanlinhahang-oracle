using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Quanlinhahang.Data;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Admin.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("QLNH");

// ====================== CẤU HÌNH DỊCH VỤ ======================
builder.Services.AddControllersWithViews();

builder.Services.AddHostedService<BookingTimeoutService>();

builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseSqlServer(AppConfig.ConnectionString));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; 
        options.AccessDeniedPath = "/Account/AccessDenied"; 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();
builder.Services.AddHttpContextAccessor();

// ⚙️ Session (dành cho controller lưu session)
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

string sharedFolder = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "SharedImages");
sharedFolder = Path.GetFullPath(sharedFolder); // Chuẩn hóa đường dẫn

// Kiểm tra xem đã tìm đúng chưa (Tạo nếu chưa có)
if (!Directory.Exists(sharedFolder)) Directory.CreateDirectory(sharedFolder);

// Map vào đường dẫn ảo "/shared" (cho khớp với code View bạn đang dùng)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(sharedFolder),
    RequestPath = "/shared"
});

app.UseRouting();

// 🧩 Thứ tự chuẩn: Auth -> Session -> Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ====================== ROUTE ======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
