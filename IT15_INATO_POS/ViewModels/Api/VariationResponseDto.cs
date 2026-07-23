using System;

namespace IT15_INATO_POS.ViewModels.Api
{
    public class VariationResponseDto
    {
        public int VariationID { get; set; }
        public int ProductID { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockLevel { get; set; }
        public int LowStockThreshold { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}