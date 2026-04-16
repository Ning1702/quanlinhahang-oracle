using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Quanlinhahang.Data;
using Quanlinhahang.Data.Models;

var builder = WebApplication.CreateBuilder(args);

// ====================== 1. CẤU HÌNH DỊCH VỤ ======================
builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseSqlServer(AppConfig.ConnectionString));

// 🔥 CẤU HÌNH SESSION
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".QLNH.Staff.Session";
});

// SỬA LỖI 2: Đổi /TaiKhoan/ thành /Account/ cho khớp với Controller của bạn
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

// ====================== 2. BUILD APP ======================
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "SharedImages")),
    RequestPath = "/SharedImages"
});

// KÍCH HOẠT SESSION
app.UseSession();

app.UseRouting();

// SỬA LỖI 1: BẮT BUỘC PHẢI CÓ DÒNG NÀY VÀ PHẢI ĐỨNG TRƯỚC AUTHORIZATION
app.UseAuthentication();

app.UseAuthorization();

app.MapHub<Quanlinhahang_Staff.Hubs.NotificationHub>("/notificationHub");

// Cấu hình Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();