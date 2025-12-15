using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("QLNH");

// ====================== CẤU HÌNH DỊCH VỤ ======================
builder.Services.AddControllersWithViews();

// Kết nối DB (Giữ nguyên connection string của Staff)
builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("QLNH")));

// 🔥 QUAN TRỌNG: Cấu hình DataProtection (Phải GIỐNG HỆT bên Admin)
// Để Staff hiểu được Cookie do Admin tạo ra và ngược lại
var keyDirectory = new DirectoryInfo(@"C:\QuanLyNhaHang_Keys");
if (!keyDirectory.Exists) keyDirectory.Create();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keyDirectory)
    .SetApplicationName("QuanLyNhaHangApp"); // Tên App phải trùng khớp với bên Admin

// 🧩 Cấu hình Authentication (Phải GIỐNG HỆT bên Admin)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "QLNH.Auth"; // Tên Cookie chung
        options.Cookie.Domain = "localhost"; // Chia sẻ giữa các port
        options.LoginPath = "/Account/Login"; // Đường dẫn ảo (thực tế Staff sẽ redirect sang Admin)
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

// ⚙️ Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
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

// 🧩 BẮT BUỘC: Phải có UseAuthentication trước UseAuthorization
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Invoices}/{action=Index}/{id?}"); // Mặc định Staff vào trang Hóa đơn

app.Run();