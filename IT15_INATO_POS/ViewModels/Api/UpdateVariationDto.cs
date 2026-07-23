namespace IT15_INATO_POS.ViewModels.Api
{
    public class UpdateVariationDto
    {
#pragma warning disable S6964
        public int VariationID { get; set; }
#pragma warning restore S6964
#pragma warning disable S6964
        public int ProductID { get; set; }
#pragma warning restore S6964
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
#pragma warning disable S6964
        public decimal Price { get; set; }
#pragma warning restore S6964
#pragma warning disable S6964
        public int StockLevel { get; set; }
#pragma warning restore S6964
#pragma warning disable S6964
        public int LowStockThreshold { get; set; }
#pragma warning restore S6964
        public string? ImageUrl { get; set; }
#pragma warning disable S6964
        public bool IsActive { get; set; }
#pragma warning restore S6964
    }
}