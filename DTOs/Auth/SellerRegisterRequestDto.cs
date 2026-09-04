using System.ComponentModel.DataAnnotations;

namespace Ecom.DTOs.Auth
{
    public class SellerRegisterRequestDto
    {
        // ============================================================
        // USER INFORMATION
        // ============================================================

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        // ============================================================
        // BUSINESS INFORMATION
        // ============================================================

        [Required]
        [StringLength(100)]
        public string BusinessName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string BusinessDescription { get; set; } = string.Empty;

        // ============================================================
        // VERIFICATION DOCUMENTS
        // ============================================================

        [Required]
        public string NIDDocumentUrl { get; set; } = string.Empty;

        [Required]
        public string BankDocumentUrl { get; set; } = string.Empty;

        public string? BusinessDocumentUrl { get; set; }
    }
}