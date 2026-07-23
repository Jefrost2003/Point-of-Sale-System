using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using IT15_INATO_POS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace IT15_INATO_POS.Controllers
{
    [Authorize(Roles = "Super Admin,Cashier")]
    public class PosController : Controller
    {
        private const string ErrorMessageKey = "ErrorMessage";
        private const string CartSessionKey = "Cart";
        private const string InvalidInputDataMessage = "Invalid input data.";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly IPayMongoService _payMongoService;
        private readonly IPdfService _pdfService;

        public PosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            IPayMongoService payMongoService,
            IPdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _payMongoService = payMongoService;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.ProductVariations)
                .Where(p => p.IsActive)
                .ToListAsync();

            var viewModel = new POSViewModel
            {
                Products = products,
                CartItems = GetCartItems(),
                PaymentMethod = "Cash"
            };

            viewModel.CartTotal = viewModel.CartItems.Sum(c => c.Subtotal);
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetCart()
        {
            var cart = GetCartItems();
            var cartTotal = cart.Sum(c => c.Subtotal);
            var cartCount = cart.Sum(c => c.Quantity);

            return Json(new
            {
                success = true,
                cartItems = cart,
                cartTotal = cartTotal,
                cartCount = cartCount
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int variationId, int quantity = 1)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = InvalidInputDataMessage });
            }

            var variation = await _context.ProductVariations
                .Include(pv => pv.Product)
                .FirstOrDefaultAsync(pv => pv.VariationID == variationId);

            if (variation == null || !variation.IsActive)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (variation.StockLevel < quantity)
            {
                return Json(new { success = false, message = "Insufficient stock" });
            }

            var cart = GetCartItems();
            var existingItem = cart.FirstOrDefault(c => c.VariationID == variationId);

            if (existingItem != null)
            {
                if (variation.StockLevel < existingItem.Quantity + quantity)
                {
                    return Json(new { success = false, message = "Insufficient stock" });
                }
                existingItem.Quantity += quantity;
                existingItem.Subtotal = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    VariationID = variation.VariationID,
                    ProductName = variation.Product!.ProductName,
                    Size = variation.Size,
                    Color = variation.Color,
                    UnitPrice = variation.Price,
                    Quantity = quantity,
                    Subtotal = variation.Price * quantity,
                    AvailableStock = variation.StockLevel
                });
            }

            SaveCartItems(cart);

            return Json(new
            {
                success = true,
                cartTotal = cart.Sum(c => c.Subtotal),
                cartCount = cart.Sum(c => c.Quantity),
                message = "Item added to cart"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int variationId, int quantity)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = InvalidInputDataMessage });
            }

            if (quantity < 1)
            {
                return Json(new { success = false, message = "Quantity must be at least 1" });
            }

            var variation = await _context.ProductVariations.FindAsync(variationId);
            if (variation == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (variation.StockLevel < quantity)
            {
                return Json(new { success = false, message = "Insufficient stock" });
            }

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.VariationID == variationId);

            if (item != null)
            {
                item.Quantity = quantity;
                item.Subtotal = item.Quantity * item.UnitPrice;
                SaveCartItems(cart);

                return Json(new
                {
                    success = true,
                    itemSubtotal = item.Subtotal,
                    cartTotal = cart.Sum(c => c.Subtotal)
                });
            }

            return Json(new { success = false, message = "Item not found in cart" });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int variationId)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = InvalidInputDataMessage });
            }

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.VariationID == variationId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);

                return Json(new
                {
                    success = true,
                    cartTotal = cart.Sum(c => c.Subtotal),
                    cartCount = cart.Sum(c => c.Quantity),
                    message = "Item removed from cart"
                });
            }

            return Json(new { success = false, message = "Item not found in cart" });
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return Json(new { success = true, message = "Cart cleared" });
        }

        public IActionResult Checkout()
        {
            var cart = GetCartItems();
            if (!cart.Any())
            {
                TempData[ErrorMessageKey] = "Cart is empty";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                TotalAmount = cart.Sum(c => c.Subtotal),
                PaymentMethod = "Cash"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = InvalidInputDataMessage });
            }

            var cart = GetCartItems();
            if (!cart.Any())
            {
                return Json(new { success = false, message = "Cart is empty" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Validate stock levels
            var stockValidationResult = await ValidateStockLevelsAsync(cart);
            if (!stockValidationResult.IsValid)
            {
                return Json(new { success = false, message = stockValidationResult.Message });
            }

            string? payMongoRef = null;

            // Process PayMongo payment if not cash
            if (model.PaymentMethod != "Cash")
            {
                var paymentResult = await ProcessNonCashPaymentAsync(model);
                if (!paymentResult.Success)
                {
                    return Json(new { success = false, message = paymentResult.Message });
                }
                payMongoRef = paymentResult.ReferenceId;
            }

            // Create transaction
            var transactionResult = await CreateTransactionAsync(user.Id, model, payMongoRef, cart);
            if (!transactionResult.Success)
            {
                return Json(new { success = false, message = transactionResult.Message });
            }

            HttpContext.Session.Remove(CartSessionKey);

            return Json(new
            {
                success = true,
                transactionId = transactionResult.TransactionId,
                message = "Transaction completed successfully"
            });
        }

        public async Task<IActionResult> Receipt(int id)
        {
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageKey] = "Invalid request data.";
                return RedirectToAction(nameof(Index));
            }

            var transaction = await _context.Transactions
                .Include(t => t.Cashier)
                .Include(t => t.SalesItems!)
                    .ThenInclude(si => si.ProductVariation!)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(t => t.TransID == id);

            if (transaction == null)
            {
                TempData[ErrorMessageKey] = "Transaction not found";
                return RedirectToAction(nameof(Index));
            }

            return View(transaction);
        }

        public async Task<IActionResult> DownloadReceipt(int id)
        {
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageKey] = "Invalid request data.";
                return RedirectToAction(nameof(Index));
            }

            var pdfBytes = await _pdfService.GenerateReceiptPdfAsync(id);

            if (pdfBytes == null)
            {
                TempData[ErrorMessageKey] = "Failed to generate receipt PDF";
                return RedirectToAction(nameof(Receipt), new { id });
            }

            return File(pdfBytes, "application/pdf", $"Receipt_{id}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private List<CartItemViewModel> GetCartItems()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItemViewModel>();
            }

            return JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartJson) ?? new List<CartItemViewModel>();
        }

        private void SaveCartItems(List<CartItemViewModel> cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString(CartSessionKey, cartJson);
        }

        private async Task<(bool IsValid, string Message)> ValidateStockLevelsAsync(List<CartItemViewModel> cart)
        {
            foreach (var item in cart)
            {
                var variation = await _context.ProductVariations.FindAsync(item.VariationID);
                if (variation == null)
                {
                    return (false, $"Product not found");
                }
                if (variation.StockLevel < item.Quantity)
                {
                    return (false, $"Insufficient stock for {item.ProductName}");
                }
            }
            return (true, string.Empty);
        }

        private async Task<(bool Success, string? ReferenceId, string Message)> ProcessNonCashPaymentAsync(CheckoutViewModel model)
        {
            try
            {
                var (success, referenceId, errorMessage) = await _payMongoService.CreatePaymentIntentAsync(
                    model.TotalAmount,
                    $"OOTD Purchase - Order #{DateTime.Now.Ticks}",
                    model.PaymentMethod);

                if (!success)
                {
                    return (false, null, $"Payment processing failed: {errorMessage}");
                }

                return (true, referenceId, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, null, $"Payment error: {ex.Message}");
            }
        }

        private async Task<(bool Success, int? TransactionId, string Message)> CreateTransactionAsync(
            string userId, CheckoutViewModel model, string? payMongoRef, List<CartItemViewModel> cart)
        {
            try
            {
                var transaction = new Transaction
                {
                    CashierID = userId,
                    TotalAmount = model.TotalAmount,
                    AmountPaid = model.AmountPaid,
                    ChangeAmount = model.ChangeAmount,
                    PaymentMethod = model.PaymentMethod,
                    PayMongoReference = payMongoRef,
                    TransactionStatus = "Completed",
                    TransactionDate = DateTime.Now,
                    Notes = model.Notes
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var salesItem = new SalesItem
                    {
                        TransID = transaction.TransID,
                        VariationID = item.VariationID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        CreatedDate = DateTime.Now,
                        CreatedBy = userId
                    };

                    _context.SalesItems.Add(salesItem);

                    var variation = await _context.ProductVariations.FindAsync(item.VariationID);
                    if (variation != null)
                    {
                        variation.StockLevel -= item.Quantity;
                        _context.Update(variation);
                    }
                }

                await _context.SaveChangesAsync();

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _auditLogService.LogActionAsync(
                        userId,
                        $"Completed transaction #{transaction.TransID}",
                        "POS",
                        $"Amount: ₱{transaction.TotalAmount:N2}, Payment: {transaction.PaymentMethod}");
                }

                return (true, transaction.TransID, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, null, $"Transaction error: {ex.Message}");
            }
        }
    }
}