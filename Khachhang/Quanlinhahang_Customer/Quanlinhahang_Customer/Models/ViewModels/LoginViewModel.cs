using System.ComponentModel.DataAnnotations;

namespace Quanlinhahang_Customer.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty; 
        public bool RememberMe { get; set; }
    }
}
