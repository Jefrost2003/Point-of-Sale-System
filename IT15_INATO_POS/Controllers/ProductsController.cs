using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using IT15_INATO_POS.Utilities;
using IT15_INATO_POS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT15_INATO_POS.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly ISystemLogService _systemLogService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(
            ApplicationDbContext context,
            IAuditLogService auditLogService,
            ISystemLogService systemLogService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditLogService = auditLogService;
            _systemLogService = systemLogService;
            _userManager = userManager;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.ProductVariations)
                    .Where(p => p.IsActive)
                    .ToListAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error loading products: {ex.Message}";
                return View(new List<Product>());
            }
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id == null) return NotFound();

            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductVariations)
                    .FirstOrDefaultAsync(m => m.ProductID == id);

                if (product == null) return NotFound();
                return View(product);
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error loading product details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Products/Create
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public IActionResult Create() => View(new ProductViewModel());

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            try
            {
                ModelState.Remove("Variations");
                if (!ModelState.IsValid) return View(model);

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData[Constants.ErrorMessage] = "User not found. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.SKU == model.SKU);
                if (existingProduct != null)
                {
                    ModelState.AddModelError("SKU", "A product with this SKU already exists.");
                    return View(model);
                }

                var product = new Product
                {
                    ProductName = SanitizeInput(model.ProductName?.Trim() ?? string.Empty),
                    SKU = SanitizeInput(model.SKU?.Trim() ?? string.Empty),
                    Description = SanitizeInput(model.Description?.Trim() ?? string.Empty),
                    ImageUrl = string.IsNullOrEmpty(model.ImageUrl) ? "/images/products/placeholder.jpg" : model.ImageUrl,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = user.Id
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

#pragma warning disable S1192
                await _systemLogService.LogAsync(
                    user.Id,
                    "Products",
                    "Create",
                    $"Created product: {product.ProductName} (SKU: {product.SKU})"
                );
#pragma warning restore S1192

                await _auditLogService.LogActionAsync(
                    user.Id,
                    $"Created product: {product.ProductName}",
                    Constants.ProductsModule,
                    $"SKU: {product.SKU}"
                );

                TempData[Constants.SuccessMessage] = $"Product '{product.ProductName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData[Constants.ErrorMessage] = $"Database error: {innerMessage}";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error creating product: {ex.Message}";
                return View(model);
            }
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id == null) return NotFound();

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                var model = new ProductViewModel
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    SKU = product.SKU,
                    Description = product.Description,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error loading product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
#pragma warning disable S3776
        public async Task<IActionResult> Edit(int id, ProductViewModel model, string submitAction)
#pragma warning restore S3776
        {
            if (id != model.ProductID) return NotFound();

            try
            {
                ModelState.Remove("Variations");
                if (!ModelState.IsValid) return View(model);

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData[Constants.ErrorMessage] = "User not found. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                if (submitAction == "archive" && product.IsActive)
                {
                    var variationCount = await _context.ProductVariations.CountAsync(pv => pv.ProductID == product.ProductID);

                    var alreadyArchived = await _context.Archive.AnyAsync(a => a.OriginalProductID == product.ProductID);
                    if (!alreadyArchived)
                    {
                        var archivedProduct = new Archive
                        {
                            OriginalProductID = product.ProductID,
                            ProductName = product.ProductName,
                            SKU = product.SKU,
                            Description = product.Description,
                            ImageUrl = product.ImageUrl,
                            VariationCount = variationCount,
                            ArchivedDate = DateTime.Now,
                            ArchivedBy = user.Id
                        };
                        _context.Archive.Add(archivedProduct);
                    }

                    product.IsActive = false;
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    await _systemLogService.LogAsync(
                        user.Id,
                        "Products",
                        "Archive",
                        $"Archived product: {product.ProductName} (SKU: {product.SKU})"
                    );

                    await _auditLogService.LogActionAsync(
                        user.Id,
                        $"Archived product: {product.ProductName}",
                        Constants.ProductsModule,
                        $"SKU: {product.SKU}"
                    );

                    TempData[Constants.SuccessMessage] = "Product archived successfully!";
                    return RedirectToAction(nameof(Index));
                }

                if (product.IsActive && !model.IsActive)
                {
                    var variationCount = await _context.ProductVariations.CountAsync(pv => pv.ProductID == product.ProductID);

                    var alreadyArchived = await _context.Archive.AnyAsync(a => a.OriginalProductID == product.ProductID);
                    if (!alreadyArchived)
                    {
                        var archivedProduct = new Archive
                        {
                            OriginalProductID = product.ProductID,
                            ProductName = product.ProductName,
                            SKU = product.SKU,
                            Description = product.Description,
                            ImageUrl = product.ImageUrl,
                            VariationCount = variationCount,
                            ArchivedDate = DateTime.Now,
                            ArchivedBy = user.Id
                        };
                        _context.Archive.Add(archivedProduct);
                    }
                }

                product.ProductName = SanitizeInput(model.ProductName.Trim());
                product.SKU = SanitizeInput(model.SKU.Trim());
                product.Description = SanitizeInput(model.Description?.Trim() ?? string.Empty);
                product.ImageUrl = string.IsNullOrEmpty(model.ImageUrl) ? "/images/products/placeholder.jpg" : model.ImageUrl;
                product.IsActive = model.IsActive;
                product.ModifiedDate = DateTime.Now;
                product.ModifiedBy = user.Id;

                _context.Update(product);
                await _context.SaveChangesAsync();

                await _systemLogService.LogAsync(
                    user.Id,
                    "Products",
                    "Update",
                    $"Updated product: {product.ProductName} (SKU: {product.SKU}, Active: {product.IsActive})"
                );

                await _auditLogService.LogActionAsync(
                    user.Id,
                    $"Updated product: {product.ProductName}",
                    Constants.ProductsModule,
                    $"SKU: {product.SKU}, Active: {product.IsActive}"
                );

                TempData[Constants.SuccessMessage] = product.IsActive ? "Product updated successfully!" : "Product has been archived.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData[Constants.ErrorMessage] = $"Database error: {innerMessage}";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error updating product: {ex.Message}";
                return View(model);
            }
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id == null) return NotFound();

            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductVariations)
                    .FirstOrDefaultAsync(m => m.ProductID == id);

                if (product == null) return NotFound();
                return View(product);
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error loading product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        TempData[Constants.ErrorMessage] = "User not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    var variationCount = await _context.ProductVariations.CountAsync(pv => pv.ProductID == product.ProductID);

                    var archivedProduct = new Archive
                    {
                        OriginalProductID = product.ProductID,
                        ProductName = product.ProductName,
                        SKU = product.SKU,
                        Description = product.Description,
                        ImageUrl = product.ImageUrl,
                        VariationCount = variationCount,
                        ArchivedDate = DateTime.Now,
                        ArchivedBy = user.Id
                    };
                    _context.Archive.Add(archivedProduct);

                    product.IsActive = false;
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    await _systemLogService.LogAsync(
                        user.Id,
                        "Products",
                        "Delete",
                        $"Archived product: {product.ProductName} (SKU: {product.SKU})"
                    );

                    await _auditLogService.LogActionAsync(
                        user.Id,
                        $"Archived product: {product.ProductName}",
                        Constants.ProductsModule,
                        $"SKU: {product.SKU}"
                    );

                    TempData[Constants.SuccessMessage] = "Product archived successfully!";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error archiving product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Products/Variations/5
        public async Task<IActionResult> Variations(int? id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id == null) return NotFound();

            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductVariations)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null) return NotFound();

                ViewBag.ProductName = product.ProductName;
                ViewBag.ProductID = product.ProductID;
                return View(product.ProductVariations ?? new List<ProductVariation>());
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error loading variations: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Products/CreateVariation/5
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
#pragma warning disable S6967
        public IActionResult CreateVariation(int productId) => View(new ProductVariationViewModel { ProductID = productId });
#pragma warning restore S6967

        // POST: Products/CreateVariation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public async Task<IActionResult> CreateVariation(ProductVariationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                var existingVariation = await _context.ProductVariations
                    .FirstOrDefaultAsync(pv => pv.ProductID == model.ProductID && pv.Size == model.Size && pv.Color == model.Color);

                if (existingVariation != null)
                {
                    ModelState.AddModelError(string.Empty, "A variation with this size and color already exists for this product.");
                    return View(model);
                }

                var variation = new ProductVariation
                {
                    ProductID = model.ProductID,
                    Size = SanitizeInput(model.Size?.Trim() ?? string.Empty),
                    Color = SanitizeInput(model.Color?.Trim() ?? string.Empty),
                    Price = model.Price,
                    StockLevel = model.StockLevel,
                    LowStockThreshold = model.LowStockThreshold,
                    ImageUrl = model.ImageUrl,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Add(variation);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _systemLogService.LogAsync(
                        user.Id,
                        "ProductVariations",
                        "Create",
                        $"Created variation for Product ID: {model.ProductID}, Size: {model.Size}, Color: {model.Color}"
                    );

                    await _auditLogService.LogActionAsync(
                        user.Id,
                        "Created product variation",
                        Constants.ProductVariationsModule,
                        $"Product ID: {model.ProductID}, Size: {model.Size}, Color: {model.Color}"
                    );
                }

                TempData[Constants.SuccessMessage] = "Product variation created successfully!";
                return RedirectToAction(nameof(Variations), new { id = model.ProductID });
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error creating variation: {ex.Message}";
                return View(model);
            }
        }

        // GET: Products/EditVariation/5
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public async Task<IActionResult> EditVariation(int? id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id == null) return NotFound();

            try
            {
                var variation = await _context.ProductVariations.FindAsync(id);
                if (variation == null) return NotFound();

                var model = new ProductVariationViewModel
                {
                    VariationID = variation.VariationID,
                    ProductID = variation.ProductID,
                    Size = variation.Size,
                    Color = variation.Color,
                    Price = variation.Price,
                    StockLevel = variation.StockLevel,
                    LowStockThreshold = variation.LowStockThreshold,
                    ImageUrl = variation.ImageUrl,
                    IsActive = variation.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error loading variation: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/EditVariation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public async Task<IActionResult> EditVariation(int id, ProductVariationViewModel model)
        {
            if (id != model.VariationID) return NotFound();

            try
            {
                if (!ModelState.IsValid) return View(model);

                var variation = await _context.ProductVariations.FindAsync(id);
                if (variation == null) return NotFound();

                variation.Size = SanitizeInput(model.Size?.Trim() ?? string.Empty);
                variation.Color = SanitizeInput(model.Color?.Trim() ?? string.Empty);
                variation.Price = model.Price;
                variation.StockLevel = model.StockLevel;
                variation.LowStockThreshold = model.LowStockThreshold;
                variation.ImageUrl = model.ImageUrl;
                variation.IsActive = model.IsActive;

                _context.Update(variation);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _systemLogService.LogAsync(
                        user.Id,
                        "ProductVariations",
                        "Update",
                        $"Updated variation ID: {id}"
                    );

                    await _auditLogService.LogActionAsync(
                        user.Id,
                        "Updated product variation",
                        Constants.ProductVariationsModule,
                        $"Variation ID: {id}"
                    );
                }

                TempData[Constants.SuccessMessage] = "Product variation updated successfully!";
                return RedirectToAction(nameof(Variations), new { id = model.ProductID });
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error updating variation: {ex.Message}";
                return View(model);
            }
        }

        // GET: Products/Archived (Combined Products and Users)
        public async Task<IActionResult> Archived()
        {
            var viewModel = new CombinedArchiveViewModel
            {
                ArchivedProducts = await _context.Archive
                    .Include(a => a.ArchivedByUser)
                    .OrderByDescending(a => a.ArchivedDate)
                    .ToListAsync(),
                ArchivedUsers = await _context.ArchivedUsers
                    .Include(a => a.ArchivedByUser)
                    .OrderByDescending(a => a.ArchivedDate)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Products/RestoreProduct/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var archivedProduct = await _context.Archive.FindAsync(id);
                if (archivedProduct == null)
                {
                    TempData[Constants.ErrorMessage] = "Archived product not found.";
                    return RedirectToAction(nameof(Archived));
                }

                var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.SKU == archivedProduct.SKU);
                if (existingProduct != null && existingProduct.IsActive)
                {
                    TempData[Constants.ErrorMessage] = $"Product with SKU {archivedProduct.SKU} already exists. Cannot restore.";
                    return RedirectToAction(nameof(Archived));
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData[Constants.ErrorMessage] = "User not found.";
                    return RedirectToAction(nameof(Archived));
                }

                Product restoredProduct;

                if (existingProduct != null && !existingProduct.IsActive)
                {
                    existingProduct.IsActive = true;
                    existingProduct.ModifiedDate = DateTime.Now;
                    existingProduct.ModifiedBy = user.Id;
                    restoredProduct = existingProduct;
                    _context.Update(restoredProduct);
                }
                else
                {
                    restoredProduct = new Product
                    {
                        ProductName = archivedProduct.ProductName,
                        SKU = archivedProduct.SKU,
                        Description = archivedProduct.Description,
                        ImageUrl = archivedProduct.ImageUrl,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = user.Id
                    };
                    _context.Products.Add(restoredProduct);
                }

                await _context.SaveChangesAsync();

                _context.Archive.Remove(archivedProduct);
                await _context.SaveChangesAsync();

                await _systemLogService.LogAsync(
                    user.Id,
                    "Products",
                    "Restore",
                    $"Restored product: {restoredProduct.ProductName} (SKU: {restoredProduct.SKU})"
                );

                await _auditLogService.LogActionAsync(
                    user.Id,
                    $"Restored product: {restoredProduct.ProductName}",
                    Constants.ProductsModule,
                    $"SKU: {restoredProduct.SKU}"
                );

                TempData[Constants.SuccessMessage] = $"Product '{restoredProduct.ProductName}' restored successfully!";
                return RedirectToAction(nameof(Archived));
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error restoring product: {ex.Message}";
                return RedirectToAction(nameof(Archived));
            }
        }

        // POST: Products/PermanentlyDeleteArchivedProduct/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentlyDeleteArchivedProduct(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var archivedProduct = await _context.Archive.FindAsync(id);
                if (archivedProduct == null)
                {
                    TempData[Constants.ErrorMessage] = "Archived product not found.";
                    return RedirectToAction(nameof(Archived));
                }

                _context.Archive.Remove(archivedProduct);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _systemLogService.LogAsync(
                        user.Id,
                        "Products",
                        "PermanentDelete",
                        $"Permanently deleted archived product: {archivedProduct.ProductName} (SKU: {archivedProduct.SKU})"
                    );

                    await _auditLogService.LogActionAsync(
                        user.Id,
                        $"Permanently deleted archived product: {archivedProduct.ProductName}",
                        Constants.ProductsModule,
                        $"SKU: {archivedProduct.SKU}"
                    );
                }

                TempData[Constants.SuccessMessage] = "Archived product permanently deleted.";
                return RedirectToAction(nameof(Archived));
            }
            catch (Exception ex)
            {
                TempData[Constants.ErrorMessage] = $"Error deleting archived product: {ex.Message}";
                return RedirectToAction(nameof(Archived));
            }
        }

        // Input sanitization method to prevent XSS
        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&#39;");
        }
    }
}