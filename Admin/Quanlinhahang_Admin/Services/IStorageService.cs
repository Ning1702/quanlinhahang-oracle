using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Quanlinhahang_Admin.Services
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(IFormFile file);
        Task DeleteFileAsync(string fileName);
    }
}