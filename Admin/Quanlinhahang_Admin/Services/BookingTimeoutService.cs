using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;

namespace Quanlinhahang_Admin.Services
{
    public class BookingTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public BookingTimeoutService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var _context = scope.ServiceProvider.GetRequiredService<QuanLyNhaHangContext>();

                        // Dùng UTC để tương thích với PostgreSQL timestamp with time zone
                        var timeoutLimit = DateTime.UtcNow.AddMinutes(-10);

                        var expiredBookings = await _context.DatBans
                            .Include(d => d.BanPhong)
                            .Include(d => d.HoaDon)
                            .Where(d => d.TrangThaiId == 1
                                     && d.ThoiGianTaoDon != null
                                     && d.ThoiGianTaoDon < timeoutLimit)
                            .ToListAsync(stoppingToken);

                        foreach (var booking in expiredBookings)
                        {
                            booking.TrangThaiId = 5;

                            if (booking.HoaDon != null)
                            {
                                booking.HoaDon.TrangThaiId = 5;
                            }

                            if (booking.BanPhong != null)
                            {
                                booking.BanPhong.TrangThaiId = 0;
                            }
                        }

                        if (expiredBookings.Any())
                        {
                            await _context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Lỗi BookingTimeoutService]: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}