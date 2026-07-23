using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace IT15_INATO_POS.Controllers
{
    [Authorize]
    public class HomeController(ApplicationDbContext context) : Controller
    {
        private const string CompletedStatus = "Completed";
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TodaySales = await GetTodaySalesAsync(),
                WeeklySales = await GetWeeklySalesAsync(),
                MonthlySales = await GetMonthlySalesAsync(),
                TodayTransactions = await GetTodayTransactionsCountAsync(),
                LowStockItemsCount = await GetLowStockItemsCountAsync(),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalUsers = await _context.Users.CountAsync(u => u.IsActive),
                RecentTransactions = await GetRecentTransactionsAsync(),
                TopSellingProducts = await GetTopSellingProductsAsync(),
                SalesChartData = await GetSalesChartDataAsync()
            };

            return View(viewModel);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<decimal> GetTodaySalesAsync()
        {
            var today = DateTime.Today;
            return await _context.Transactions
                .Where(t => t.TransactionDate.Date == today && t.TransactionStatus == CompletedStatus)
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;
        }

        private async Task<decimal> GetWeeklySalesAsync()
        {
            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            return await _context.Transactions
                .Where(t => t.TransactionDate >= weekStart && t.TransactionStatus == CompletedStatus)
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;
        }

        private async Task<decimal> GetMonthlySalesAsync()
        {
            // Fixed: Specify DateTimeKind to avoid S6562 warning
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Local);
            return await _context.Transactions
                .Where(t => t.TransactionDate >= monthStart && t.TransactionStatus == CompletedStatus)
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;
        }

        private async Task<int> GetTodayTransactionsCountAsync()
        {
            var today = DateTime.Today;
            return await _context.Transactions.CountAsync(t => t.TransactionDate.Date == today);
        }

        private async Task<int> GetLowStockItemsCountAsync()
        {
            return await _context.ProductVariations.CountAsync(pv => pv.StockLevel <= pv.LowStockThreshold && pv.IsActive);
        }

        private async Task<List<RecentTransactionViewModel>> GetRecentTransactionsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Cashier)
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .Select(t => new RecentTransactionViewModel
                {
                    TransID = t.TransID,
                    CashierName = t.Cashier != null ? t.Cashier.FullName : "Unknown",
                    TotalAmount = t.TotalAmount,
                    PaymentMethod = t.PaymentMethod,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();
        }

        private async Task<List<TopSellingProductViewModel>> GetTopSellingProductsAsync()
        {
            var last30Days = DateTime.Today.AddDays(-30);
            return await _context.SalesItems
                .Include(si => si.ProductVariation)
                    .ThenInclude(pv => pv!.Product)
                .Where(si => si.Transaction != null && si.Transaction.TransactionDate >= last30Days)
                .GroupBy(si => si.ProductVariation!.Product!.ProductName)
                .Select(g => new TopSellingProductViewModel
                {
                    ProductName = g.Key,
                    TotalQuantitySold = g.Sum(si => si.Quantity),
                    TotalRevenue = g.Sum(si => si.Subtotal)
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<SalesChartDataViewModel>> GetSalesChartDataAsync()
        {
            var last7Days = DateTime.Today.AddDays(-6);
            var salesData = await _context.Transactions
                .Where(t => t.TransactionDate.Date >= last7Days && t.TransactionStatus == CompletedStatus)
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new SalesChartDataViewModel
                {
                    Date = g.Key.ToString("MMM dd"),
                    Amount = g.Sum(t => t.TotalAmount)
                })
                .ToListAsync();

            var result = new List<SalesChartDataViewModel>();
            for (int i = 0; i < 7; i++)
            {
                var date = last7Days.AddDays(i);
                var existing = salesData.FirstOrDefault(s => s.Date == date.ToString("MMM dd"));
                result.Add(existing ?? new SalesChartDataViewModel { Date = date.ToString("MMM dd"), Amount = 0 });
            }

            return result;
        }
    }
}