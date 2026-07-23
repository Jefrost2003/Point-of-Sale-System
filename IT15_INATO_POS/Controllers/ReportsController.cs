using IT15_INATO_POS.Data;
using IT15_INATO_POS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_INATO_POS.Controllers
{
    [Authorize(Roles = "Super Admin,Admin,Store Manager")]
    public class ReportsController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private const string DateFormat = "yyyy-MM-dd";

        // GET: Reports
        public IActionResult Index() => View();

        // GET: Reports/Sales
        public IActionResult Sales(DateTime? startDate, DateTime? endDate)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            ViewBag.StartDate = startDate.Value.ToString(DateFormat);
            ViewBag.EndDate = endDate.Value.ToString(DateFormat);

            var viewModel = GetSalesReport(startDate.Value, endDate.Value);
            return View(viewModel);
        }

        // GET: Reports/Inventory
        public async Task<IActionResult> Inventory()
        {
            var inventoryReport = await _context.Products
                .Include(p => p.ProductVariations)
                .Where(p => p.IsActive)
                .Select(p => new InventoryReportViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    SKU = p.SKU,
                    TotalStock = p.ProductVariations!.Sum(pv => pv.StockLevel),
                    LowStockVariations = p.ProductVariations!.Count(pv => pv.StockLevel <= pv.LowStockThreshold),
                    TotalValue = p.ProductVariations!.Sum(pv => pv.StockLevel * pv.Price),
                    Variations = p.ProductVariations!.Select(pv => new InventoryVariationViewModel
                    {
                        VariationID = pv.VariationID,
                        Size = pv.Size,
                        Color = pv.Color,
                        StockLevel = pv.StockLevel,
                        LowStockThreshold = pv.LowStockThreshold,
                        Price = pv.Price,
                        IsLowStock = pv.StockLevel <= pv.LowStockThreshold
                    }).ToList()
                })
                .ToListAsync();

            return View(inventoryReport);
        }

        // GET: Reports/LowStock
        public async Task<IActionResult> LowStock()
        {
            var lowStockItems = await _context.ProductVariations
                .Include(pv => pv.Product)
                .Where(pv => pv.StockLevel <= pv.LowStockThreshold && pv.IsActive)
                .OrderBy(pv => pv.StockLevel)
                .ToListAsync();

            return View(lowStockItems);
        }

        // GET: Reports/TopSelling
        public IActionResult TopSelling(DateTime? startDate, DateTime? endDate)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            ViewBag.StartDate = startDate.Value.ToString(DateFormat);
            ViewBag.EndDate = endDate.Value.ToString(DateFormat);

            var topSellingProducts = _context.SalesItems
                .Include(si => si.ProductVariation)
                    .ThenInclude(pv => pv!.Product)
                .Include(si => si.Transaction)
                .Where(si => si.Transaction!.TransactionDate >= startDate.Value &&
                            si.Transaction.TransactionDate <= endDate.Value &&
                            si.Transaction.TransactionStatus == "Completed")
                .GroupBy(si => new
                {
                    si.ProductVariation!.Product!.ProductID,
                    si.ProductVariation.Product.ProductName
                })
                .Select(g => new TopSellingProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    TotalQuantitySold = g.Sum(si => si.Quantity),
                    TotalRevenue = g.Sum(si => si.Subtotal)
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(20)
                .ToList();

            return View(topSellingProducts);
        }

        // GET: Reports/Transactions
        public async Task<IActionResult> Transactions(DateTime? startDate, DateTime? endDate)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            startDate ??= DateTime.Today;
            endDate ??= DateTime.Today;

            ViewBag.StartDate = startDate.Value.ToString(DateFormat);
            ViewBag.EndDate = endDate.Value.ToString(DateFormat);

            var transactions = await _context.Transactions
                .Include(t => t.Cashier)
                .Include(t => t.SalesItems)
                .Where(t => t.TransactionDate.Date >= startDate.Value.Date &&
                           t.TransactionDate.Date <= endDate.Value.Date)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return View(transactions);
        }

        // API: Get Sales Chart Data
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public IActionResult GetSalesChartData(DateTime startDate, DateTime endDate)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var salesData = _context.Transactions
                .Where(t => t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.TransactionStatus == "Completed")
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("MMM dd"),
                    Amount = g.Sum(t => t.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Json(salesData);
        }

        // Helper Methods
        private SalesReportViewModel GetSalesReport(DateTime startDate, DateTime endDate)
        {
            var transactions = _context.Transactions
                .Where(t => t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.TransactionStatus == "Completed")
                .ToList();

            var totalSales = transactions.Sum(t => t.TotalAmount);
            var totalTransactions = transactions.Count;
            var averageTransaction = totalTransactions > 0 ? totalSales / totalTransactions : 0;

            var dailyDetails = transactions
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new SalesReportDetailViewModel
                {
                    Date = g.Key,
                    TransactionCount = g.Count(),
                    DailySales = g.Sum(t => t.TotalAmount)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var paymentBreakdown = transactions
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new PaymentMethodBreakdownViewModel
                {
                    PaymentMethod = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(t => t.TotalAmount)
                })
                .ToList();

            return new SalesReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalSales = totalSales,
                TotalTransactions = totalTransactions,
                AverageTransactionValue = averageTransaction,
                Details = dailyDetails,
                PaymentMethodBreakdown = paymentBreakdown
            };
        }
    }
}