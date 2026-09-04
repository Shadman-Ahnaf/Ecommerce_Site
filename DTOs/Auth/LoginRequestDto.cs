using System.ComponentModel.DataAnnotations;

namespace Ecom.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}