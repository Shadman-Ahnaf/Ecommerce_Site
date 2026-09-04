using System;

namespace Ecom.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public UserResponseDto User { get; set; } = new UserResponseDto();
    }
}