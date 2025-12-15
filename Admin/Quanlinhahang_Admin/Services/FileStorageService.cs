using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Quanlinhahang_Admin.Services
{
    public class FileStorageService : IStorageService
    {
        private readonly string _userContentFolder;

        public FileStorageService(IConfiguration configuration, IWebHostEnvironment env)
        {
            string? pathConfig = configuration["FileStorage:UploadPath"];
            string finalPath = pathConfig ?? "wwwroot";
            _userContentFolder = Path.GetFullPath(Path.Combine(env.ContentRootPath, finalPath));

            if (!Directory.Exists(_userContentFolder))
            {
                Directory.CreateDirectory(_userContentFolder);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            // Kiểm tra null an toàn cho ContentDisposition
            var contentDisposition = file.ContentDisposition;
            string originalFileName = "unknown.jpg";

            if (contentDisposition != null)
            {
                var parsed = ContentDispositionHeaderValue.Parse(contentDisposition);
                if (parsed.FileName != null)
                {
                    originalFileName = parsed.FileName.Trim('"');
                }
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            var filePath = Path.Combine(_userContentFolder, fileName);

            using (var output = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(output);
            }
            return fileName;
        }

        public async Task DeleteFileAsync(string fileName)
        {
            var filePath = Path.Combine(_userContentFolder, fileName);
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }
    }
}