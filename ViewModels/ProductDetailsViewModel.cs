using Ecom.Models;

namespace Ecom.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;

        public string SellerName { get; set; } = string.Empty;

        public List<Review> Reviews { get; set; } = new();

        public List<Product> RelatedProducts { get; set; } = new();

        public Review NewReview { get; set; } = new();

        public bool HasReviewed { get; set; }
    }
}