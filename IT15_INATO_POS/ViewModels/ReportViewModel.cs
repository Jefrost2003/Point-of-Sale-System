namespace IT15_INATO_POS.ViewModels
{
    public class SalesReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public List<SalesReportDetailViewModel> Details { get; set; } = new();
        public List<PaymentMethodBreakdownViewModel> PaymentMethodBreakdown { get; set; } = new();
    }

    public class SalesReportDetailViewModel
    {
        public DateTime Date { get; set; }
        public int TransactionCount { get; set; }
        public decimal DailySales { get; set; }
    }

    public class PaymentMethodBreakdownViewModel
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Total { get; set; }
    }

    public class InventoryReportViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int TotalStock { get; set; }
        public int LowStockVariations { get; set; }
        public decimal TotalValue { get; set; }
        public List<InventoryVariationViewModel> Variations { get; set; } = new();
    }

    public class InventoryVariationViewModel
    {
        public int VariationID { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockLevel { get; set; }
        public int LowStockThreshold { get; set; }
        public decimal Price { get; set; }
        public bool IsLowStock { get; set; }
    }
}