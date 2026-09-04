using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecom.DTOs.Auth;
using Ecom.Models;
using EcomDB.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecom.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ============================================================
        // CUSTOMER REGISTRATION
        // ============================================================

        public async Task<AuthResponseDto> RegisterAsync(
            RegisterRequestDto request)
        {
            string email = request.Email.Trim().ToLower();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                throw new Exception(
                    "Email is already registered.");
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                string phone = request.Phone.Trim();

                bool phoneExists = await _context.Users
                    .AnyAsync(u => u.Phone == phone);

                if (phoneExists)
                {
                    throw new Exception(
                        "Phone number is already registered.");
                }
            }

            User user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                Phone = string.IsNullOrWhiteSpace(request.Phone)
                    ? null
                    : request.Phone.Trim(),

                PasswordHash = HashPassword(request.Password),

                Role = UserRole.Buyer,

                CreatedAt = DateTime.UtcNow,

                IsActive = true
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return await CreateAuthResponseAsync(user);
        }

        // ============================================================
        // CUSTOMER LOGIN
        // ============================================================

        public async Task<AuthResponseDto?> LoginAsync(
            LoginRequestDto request)
        {
            string login = request.EmailOrPhone.Trim();

            string? normalizedEmail = login.Contains("@")
                ? login.ToLower()
                : null;

            User? user;

            if (normalizedEmail != null)
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Email.ToLower() == normalizedEmail);
            }
            else
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Phone == login);
            }

            if (user == null)
            {
                return null;
            }

            if (!user.IsActive)
            {
                return null;
            }

            // Only Buyer accounts can use Customer Login.
            if (user.Role != UserRole.Buyer)
            {
                return null;
            }

            // If this user has a pending seller application,
            // don't allow the account to be used as a customer
            // through the customer login while verification is pending.
            bool hasPendingSellerApplication =
                await _context.SellerApplications
                    .AnyAsync(sa =>
                        sa.UserId == user.UserId &&
                        sa.Status == "Pending");

            if (hasPendingSellerApplication)
            {
                return null;
            }

            string hashedPassword =
                HashPassword(request.Password);

            if (user.PasswordHash != hashedPassword)
            {
                return null;
            }

            return await CreateAuthResponseAsync(user);
        }

        // ============================================================
        // SELLER REGISTRATION
        // ============================================================

        public async Task<string> RegisterSellerAsync(
            SellerRegisterRequestDto request)
        {
            string email = request.Email.Trim().ToLower();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                throw new Exception(
                    "Email is already registered.");
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                string phone = request.Phone.Trim();

                bool phoneExists = await _context.Users
                    .AnyAsync(u => u.Phone == phone);

                if (phoneExists)
                {
                    throw new Exception(
                        "Phone number is already registered.");
                }
            }

            // --------------------------------------------------------
            // Create User
            // --------------------------------------------------------

            User user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                Phone = string.IsNullOrWhiteSpace(request.Phone)
                    ? null
                    : request.Phone.Trim(),

                PasswordHash = HashPassword(request.Password),

                // Seller remains Buyer until Admin approves.
                Role = UserRole.Buyer,

                CreatedAt = DateTime.UtcNow,

                IsActive = true
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // Create Seller Application
            // --------------------------------------------------------

            SellerApplication application =
                new SellerApplication
                {
                    UserId = user.UserId,

                    BusinessName =
                        request.BusinessName.Trim(),

                    BusinessDescription =
                        request.BusinessDescription.Trim(),

                    NIDDocumentUrl =
                        request.NIDDocumentUrl.Trim(),

                    BankDocumentUrl =
                        request.BankDocumentUrl.Trim(),

                    BusinessDocumentUrl =
                        string.IsNullOrWhiteSpace(
                            request.BusinessDocumentUrl)
                            ? null
                            : request.BusinessDocumentUrl.Trim(),

                    Status = "Pending",

                    AppliedAt = DateTime.UtcNow
                };

            _context.SellerApplications.Add(application);

            await _context.SaveChangesAsync();

            return
                "Seller registration submitted successfully. " +
                "Your application is pending Admin verification.";
        }

        // ============================================================
        // SELLER LOGIN
        // ============================================================

        public async Task<AuthResponseDto?> LoginSellerAsync(
            LoginRequestDto request)
        {
            string login = request.EmailOrPhone.Trim();

            string? normalizedEmail = login.Contains("@")
                ? login.ToLower()
                : null;

            User? user;

            if (normalizedEmail != null)
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Email.ToLower() == normalizedEmail);
            }
            else
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Phone == login);
            }

            if (user == null)
            {
                return null;
            }

            if (!user.IsActive)
            {
                return null;
            }

            // Only approved Seller accounts can use Seller Login.
            if (user.Role != UserRole.Seller)
            {
                return null;
            }

            // Confirm that the seller has an approved application.
            bool approvedApplication =
                await _context.SellerApplications
                    .AnyAsync(sa =>
                        sa.UserId == user.UserId &&
                        sa.Status == "Approved");

            if (!approvedApplication)
            {
                return null;
            }

            string hashedPassword =
                HashPassword(request.Password);

            if (user.PasswordHash != hashedPassword)
            {
                return null;
            }

            return await CreateAuthResponseAsync(user);
        }

        // ============================================================
        // REFRESH TOKEN
        // ============================================================

        public async Task<AuthResponseDto?> RefreshTokenAsync(
            string refreshToken)
        {
            RefreshToken? storedToken =
                await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(
                        rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return null;
            }

            if (storedToken.IsRevoked)
            {
                return null;
            }

            if (storedToken.ExpiryDate <= DateTime.UtcNow)
            {
                return null;
            }

            if (!storedToken.User.IsActive)
            {
                return null;
            }

            storedToken.IsRevoked = true;

            storedToken.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await CreateAuthResponseAsync(
                storedToken.User);
        }

        // ============================================================
        // REVOKE REFRESH TOKEN
        // ============================================================

        public async Task<bool> RevokeRefreshTokenAsync(
            string refreshToken)
        {
            RefreshToken? storedToken =
                await _context.RefreshTokens
                    .FirstOrDefaultAsync(
                        rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            if (storedToken.IsRevoked)
            {
                return false;
            }

            storedToken.IsRevoked = true;

            storedToken.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        // ============================================================
        // CREATE AUTH RESPONSE
        // ============================================================

        private async Task<AuthResponseDto>
            CreateAuthResponseAsync(User user)
        {
            string accessToken =
                GenerateAccessToken(user);

            RefreshToken refreshToken =
                GenerateRefreshToken(user);

            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            DateTime expiresAt =
                DateTime.UtcNow.AddMinutes(
                    GetAccessTokenExpirationMinutes());

            return new AuthResponseDto
            {
                AccessToken = accessToken,

                RefreshToken = refreshToken.Token,

                ExpiresAt = expiresAt,

                User = new UserResponseDto
                {
                    Id = user.UserId,

                    Name = user.Name,

                    Email = user.Email,

                    Phone = user.Phone,

                    Role = user.Role.ToString()
                }
            };
        }

        // ============================================================
        // GENERATE ACCESS TOKEN
        // ============================================================

        private string GenerateAccessToken(User user)
        {
            string secretKey =
                _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key is not configured.");

            string issuer =
                _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is not configured.");

            string audience =
                _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is not configured.");

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Name),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString())
            };

            SymmetricSecurityKey key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey));

            SigningCredentials credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            DateTime expires =
                DateTime.UtcNow.AddMinutes(
                    GetAccessTokenExpirationMinutes());

            JwtSecurityToken token =
                new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: expires,
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // ============================================================
        // GENERATE REFRESH TOKEN
        // ============================================================

        private RefreshToken GenerateRefreshToken(
            User user)
        {
            byte[] randomBytes = new byte[64];

            using RandomNumberGenerator rng =
                RandomNumberGenerator.Create();

            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                UserId = user.UserId,

                Token = Convert.ToBase64String(
                    randomBytes),

                ExpiryDate =
                    DateTime.UtcNow.AddDays(7),

                CreatedAt =
                    DateTime.UtcNow,

                IsRevoked = false
            };
        }

        // ============================================================
        // JWT EXPIRATION
        // ============================================================

        private int GetAccessTokenExpirationMinutes()
        {
            return int.TryParse(
                _configuration[
                    "Jwt:AccessTokenExpirationMinutes"],
                out int minutes)
                ? minutes
                : 60;
        }

        // ============================================================
        // PASSWORD HASHING
        // ============================================================

        private string HashPassword(
            string password)
        {
            using SHA256 sha256 =
                SHA256.Create();

            byte[] bytes =
                sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(password));

            StringBuilder builder =
                new StringBuilder();

            foreach (byte b in bytes)
            {
                builder.Append(
                    b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}