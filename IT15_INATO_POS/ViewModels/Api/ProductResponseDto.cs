using System;
using System.Collections.Generic;

namespace IT15_INATO_POS.ViewModels.Api
{
    public class ProductResponseDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<ProductVariationDto> ProductVariations { get; set; } = new();
    }

    public class ProductVariationDto
    {
        public int VariationID { get; set; }
        public int ProductID { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockLevel { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ImageUrl { get; set; }
    }
}