using Ecom.Models;

namespace Ecom.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> HotProducts { get; set; } = new();

        public List<Product> NewProducts { get; set; } = new();

        public List<Product> SearchResults { get; set; } = new();

        public List<Category> Categories { get; set; } = new();

        public List<Offer> ActiveOffers { get; set; } = new();

        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public int? DateFilter { get; set; }

        public bool IsFiltering =>
            !string.IsNullOrWhiteSpace(Search) ||
            CategoryId.HasValue ||
            MinPrice.HasValue ||
            MaxPrice.HasValue ||
            DateFilter.HasValue;
    }
}