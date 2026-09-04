using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom.Models
{
    public class SellerApplication
    {
        [Key]
        public int ApplicationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

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

        // ============================================================
        // APPLICATION STATUS
        // ============================================================

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByAdminId { get; set; }
    }
}