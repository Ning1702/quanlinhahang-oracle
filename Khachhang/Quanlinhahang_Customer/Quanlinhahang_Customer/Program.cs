using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Quanlinhahang.Data.Models;

var builder = WebApplication.CreateBuilder(args);

// Lưu ý: Đảm bảo trong appsettings.json tên chuỗi kết nối là "QLNH"
var connectionString = builder.Configuration.GetConnectionString("QLNH");

builder.Services.AddDbContext<QuanLyNhaHangContext>(options =>
    options.UseOracle(connectionString, b =>
        b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)
    ));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

string sharedFolder = "";
string currentDir = builder.Environment.ContentRootPath;
for (int i = 0; i < 5; i++)
{
    string tryPath = Path.Combine(currentDir, "SharedImages");
    if (Directory.Exists(tryPath))
    {
        sharedFolder = tryPath;
        break;
    }
    var parent = Directory.GetParent(currentDir);
    if (parent == null) break;
    currentDir = parent.FullName;
}

if (!string.IsNullOrEmpty(sharedFolder))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(sharedFolder),
        RequestPath = "/shared"
    });
}
else
{
    throw new Exception($"❌ KHÔNG TÌM THẤY folder 'SharedImages' dù đã quét từ: {builder.Environment.ContentRootPath}");
}

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();