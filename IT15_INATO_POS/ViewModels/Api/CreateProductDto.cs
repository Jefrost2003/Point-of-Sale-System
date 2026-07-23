namespace IT15_INATO_POS.ViewModels.Api
{
    public class CreateProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}