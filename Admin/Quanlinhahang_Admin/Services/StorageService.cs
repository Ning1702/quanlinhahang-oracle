using Microsoft.AspNetCore.Http;

namespace Quanlinhahang_Admin.Services
{
    public class StorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public StorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "default.jpg";

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "monan");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public Task DeleteFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "default.jpg")
                return Task.CompletedTask;

            var filePath = Path.Combine(_environment.WebRootPath, "images", "monan", fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        }
    }
}