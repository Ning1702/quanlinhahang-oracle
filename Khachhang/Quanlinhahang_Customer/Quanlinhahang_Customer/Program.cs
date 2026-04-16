using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data;
using Quanlinhahang.Data.Models;

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string từ Render env hoặc appsettings.json
var connectionString = builder.Configuration.GetConnectionString("QLNH");

// DbContext
builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// BỎ đoạn ép phải có SharedImages local
// Vì ảnh của bạn đã đưa lên Supabase Storage rồi
// nên không cần throw exception nữa

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();