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
            // Vòng lặp chạy liên tục cho đến khi tắt ứng dụng
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var _context = scope.ServiceProvider.GetRequiredService<QuanLyNhaHangContext>();

                        // Cài đặt thời gian mốc: Trước thời điểm hiện tại 10 phút
                        // 💡 MẸO: Trong lúc test, bạn có thể đổi -10 thành -1 (1 phút) để đỡ phải chờ lâu
                        var timeoutLimit = DateTime.Now.AddMinutes(-10);

                        // Tìm các đơn Đặt bàn chưa xác nhận (TrangThaiId = 1) và đã quá hạn
                        var expiredBookings = await _context.DatBans
                            .Include(d => d.BanPhong)
                            .Include(d => d.HoaDon) // Kéo theo cả Hóa đơn để xử lý
                            .Where(d => d.TrangThaiId == 1
                                     && d.ThoiGianTaoDon != null
                                     && d.ThoiGianTaoDon < timeoutLimit)
                            .ToListAsync(stoppingToken);

                        foreach (var booking in expiredBookings)
                        {
                            // 1. Chuyển trạng thái Đặt bàn thành Đã hủy (5)
                            booking.TrangThaiId = 5;

                            // 2. Chuyển trạng thái Hóa đơn thành Đã hủy (5)
                            if (booking.HoaDon != null)
                            {
                                booking.HoaDon.TrangThaiId = 5;
                            }

                            // 3. Giải phóng bàn phòng về trạng thái Trống (0)
                            if (booking.BanPhong != null)
                            {
                                booking.BanPhong.TrangThaiId = 0;
                            }
                        }

                        // Lưu thay đổi vào DB nếu có đơn bị hủy
                        if (expiredBookings.Any())
                        {
                            await _context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Tránh việc lỗi kết nối DB làm chết dịch vụ ngầm
                    Console.WriteLine($"[Lỗi BookingTimeoutService]: {ex.Message}");
                }

                // Nghỉ 1 phút rồi mới quét DB tiếp (Giúp giảm tải cho Server)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}