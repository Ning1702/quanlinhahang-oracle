using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Admin.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("QLNH");

// ====================== CẤU HÌNH DỊCH VỤ ======================
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseOracle(connectionString, b =>
        b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)
    ));

builder.Services.AddTransient<IStorageService, FileStorageService>();

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

string sharedFolder = Path.Combine(builder.Environment.ContentRootPath, "..", "SharedImages", "MonAn");
sharedFolder = Path.GetFullPath(sharedFolder);
if (Directory.Exists(sharedFolder))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(sharedFolder),
        RequestPath = "/images" 
    });
}

app.UseRouting();

// 🧩 Thứ tự chuẩn: Auth -> Session -> Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ====================== ROUTE ======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
