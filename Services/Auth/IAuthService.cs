using Ecom.DTOs.Auth;

namespace Ecom.Services.Auth
{
    public interface IAuthService
    {
        // Customer
        Task<AuthResponseDto> RegisterAsync(
            RegisterRequestDto request);

        Task<AuthResponseDto?> LoginAsync(
            LoginRequestDto request);

        // Seller
        Task<string> RegisterSellerAsync(
            SellerRegisterRequestDto request);

        Task<AuthResponseDto?> LoginSellerAsync(
            LoginRequestDto request);

        // Refresh Token
        Task<AuthResponseDto?> RefreshTokenAsync(
            string refreshToken);

        Task<bool> RevokeRefreshTokenAsync(
            string refreshToken);
    }
}