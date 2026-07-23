namespace IT15_INATO_POS.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TodaySales { get; set; }
        public decimal WeeklySales { get; set; }
        public decimal MonthlySales { get; set; }
        public int TodayTransactions { get; set; }
        public int LowStockItemsCount { get; set; }
        public int TotalProducts { get; set; }
        public int TotalUsers { get; set; }
        public List<RecentTransactionViewModel> RecentTransactions { get; set; } = new();
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new();
        public List<SalesChartDataViewModel> SalesChartData { get; set; } = new();
    }

    public class RecentTransactionViewModel
    {
        public int TransID { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }

    public class TopSellingProductViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class SalesChartDataViewModel
    {
        public string Date { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}