using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace IT15_INATO_POS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("Fixed")] // ✅ Rate limiting for API
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly HtmlEncoder _htmlEncoder;

        public ProductsApiController(ApplicationDbContext context, HtmlEncoder htmlEncoder)
        {
            _context = context;
            _htmlEncoder = htmlEncoder;
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return _htmlEncoder.Encode(input.Trim());
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.ProductVariations)
                .Where(p => p.IsActive)
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariations)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            return Ok(product);
        }

        // GET: api/Products/GetAll
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.ProductVariations)
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/GetBySKU/{sku}
        [HttpGet("GetBySKU/{sku}")]
        public async Task<ActionResult<Product>> GetProductBySKU(string sku)
        {
            if (string.IsNullOrEmpty(sku))
            {
                return BadRequest(new { message = "SKU is required." });
            }

            var sanitizedSku = SanitizeInput(sku);

            var product = await _context.Products
                .Include(p => p.ProductVariations)
                .FirstOrDefaultAsync(p => p.SKU == sanitizedSku);

            if (product == null)
            {
                return NotFound(new { message = $"Product with SKU '{sanitizedSku}' not found." });
            }

            return Ok(product);
        }

        // GET: api/Products/GetVariations/{productId}
        [HttpGet("GetVariations/{productId}")]
        public async Task<ActionResult<IEnumerable<ProductVariation>>> GetVariations(int productId)
        {
            var variations = await _context.ProductVariations
                .Where(pv => pv.ProductID == productId && pv.IsActive)
                .ToListAsync();

            if (!variations.Any())
            {
                return NotFound(new { message = $"No variations found for product ID {productId}." });
            }

            return Ok(variations);
        }

        // GET: api/Products/GetLowStock
        [HttpGet("GetLowStock")]
        public async Task<ActionResult<IEnumerable<ProductVariation>>> GetLowStockItems()
        {
            var lowStockItems = await _context.ProductVariations
                .Include(pv => pv.Product)
                .Where(pv => pv.StockLevel <= pv.LowStockThreshold && pv.IsActive)
                .OrderBy(pv => pv.StockLevel)
                .ToListAsync();

            return Ok(lowStockItems);
        }
    }
}