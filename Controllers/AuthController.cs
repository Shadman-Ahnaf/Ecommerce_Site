using Ecom.DTOs.Auth;
using Ecom.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ============================================================
        // CUSTOMER REGISTER
        // ============================================================

        // POST: api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            RegisterRequestDto request)
        {
            try
            {
                AuthResponseDto response =
                    await _authService.RegisterAsync(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // CUSTOMER LOGIN
        // ============================================================

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            LoginRequestDto request)
        {
            AuthResponseDto? response =
                await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email/phone or password."
                });
            }

            return Ok(response);
        }

        // ============================================================
        // SELLER REGISTER
        // ============================================================

        // POST: api/auth/register-seller
        [HttpPost("register-seller")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterSeller(
            SellerRegisterRequestDto request)
        {
            try
            {
                string message =
                    await _authService.RegisterSellerAsync(request);

                return Ok(new
                {
                    message = message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // SELLER LOGIN
        // ============================================================

        // POST: api/auth/login-seller
        [HttpPost("login-seller")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginSeller(
            LoginRequestDto request)
        {
            AuthResponseDto? response =
                await _authService.LoginSellerAsync(request);

            if (response == null)
            {
                return Unauthorized(new
                {
                    message =
                        "Invalid credentials, account not approved, " +
                        "or seller account is inactive."
                });
            }

            return Ok(response);
        }

        // ============================================================
        // REFRESH TOKEN
        // ============================================================

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(
            [FromBody] string refreshToken)
        {
            AuthResponseDto? response =
                await _authService.RefreshTokenAsync(
                    refreshToken);

            if (response == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid or expired refresh token."
                });
            }

            return Ok(response);
        }

        // ============================================================
        // REVOKE REFRESH TOKEN
        // ============================================================

        // POST: api/auth/revoke
        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke(
            [FromBody] string refreshToken)
        {
            bool revoked =
                await _authService.RevokeRefreshTokenAsync(
                    refreshToken);

            if (!revoked)
            {
                return BadRequest(new
                {
                    message =
                        "Invalid or already revoked refresh token."
                });
            }

            return Ok(new
            {
                message =
                    "Refresh token revoked successfully."
            });
        }
    }
}