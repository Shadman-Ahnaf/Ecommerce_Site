
using System.ComponentModel.DataAnnotations;

namespace Ecom.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}
