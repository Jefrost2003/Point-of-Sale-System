using IT15_INATO_POS.Models;
using System.ComponentModel.DataAnnotations;

namespace IT15_INATO_POS.ViewModels
{
    public class ProductViewModel
    {
#pragma warning disable S6964
        public int ProductID { get; set; }
#pragma warning restore S6964

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "SKU is required")]
        [StringLength(50)]
        [Display(Name = "SKU")]
        public string SKU { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Product Image")]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public List<ProductVariation> Variations { get; set; } = new();
    }

    public class ProductVariationViewModel
    {
#pragma warning disable S6964
        public int VariationID { get; set; }
#pragma warning restore S6964
#pragma warning disable S6964
        public int ProductID { get; set; }
#pragma warning restore S6964

        [Required(ErrorMessage = "Size is required")]
        [StringLength(20)]
        [Display(Name = "Size")]
        public string Size { get; set; } = string.Empty;

        [Required(ErrorMessage = "Color is required")]
        [StringLength(50)]
        [Display(Name = "Color")]
        public string Color { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock level is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock level cannot be negative")]
        [Display(Name = "Stock Level")]
        public int StockLevel { get; set; }

        [Display(Name = "Low Stock Threshold")]
        public int LowStockThreshold { get; set; } = 10;

        [Display(Name = "Image URL")]
        [StringLength(500)]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}