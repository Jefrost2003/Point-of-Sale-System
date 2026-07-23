using IT15_INATO_POS.Data;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using IT15_INATO_POS.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_INATO_POS.Controllers.Api
{
    [Route("api/products/{productId}/[controller]")]
    [ApiController]
    [Authorize]
    public class VariationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;

        public VariationsApiController(
            ApplicationDbContext context,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditLogService = auditLogService;
            _userManager = userManager;
        }

        // GET: api/products/{productId}/variations
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<VariationResponseDto>>> GetVariations(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound("Product not found");

            var variations = await _context.ProductVariations
                .Where(v => v.ProductID == productId && v.IsActive)
                .ToListAsync();

            var result = variations.Select(v => new VariationResponseDto
            {
                VariationID = v.VariationID,
                ProductID = v.ProductID,
                Size = v.Size,
                Color = v.Color,
                Price = v.Price,
                StockLevel = v.StockLevel,
                LowStockThreshold = v.LowStockThreshold,
                ImageUrl = v.ImageUrl,
                IsActive = v.IsActive,
                CreatedDate = v.CreatedDate
            }).ToList();

            return Ok(result);
        }

        // GET: api/variations/{id} (alternative direct access)
        [HttpGet("~/api/variations/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<VariationResponseDto>> GetVariation(int id)
        {
            var variation = await _context.ProductVariations.FindAsync(id);
            if (variation == null) return NotFound();

            var result = new VariationResponseDto
            {
                VariationID = variation.VariationID,
                ProductID = variation.ProductID,
                Size = variation.Size,
                Color = variation.Color,
                Price = variation.Price,
                StockLevel = variation.StockLevel,
                LowStockThreshold = variation.LowStockThreshold,
                ImageUrl = variation.ImageUrl,
                IsActive = variation.IsActive,
                CreatedDate = variation.CreatedDate
            };

            return Ok(result);
        }

        // POST: api/products/{productId}/variations
        [HttpPost]
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public async Task<ActionResult<VariationResponseDto>> PostVariation(int productId, CreateVariationDto dto)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound("Product not found");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var variation = new ProductVariation
            {
                ProductID = productId,
                Size = dto.Size,
                Color = dto.Color,
                Price = dto.Price,
                StockLevel = dto.StockLevel,
                LowStockThreshold = dto.LowStockThreshold,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                CreatedDate = DateTime.Now
            };

            _context.ProductVariations.Add(variation);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                user.Id,
                $"Created variation via API: {variation.Size} - {variation.Color}",
                "API",
                $"Product ID: {productId}"
            );

            var result = new VariationResponseDto
            {
                VariationID = variation.VariationID,
                ProductID = variation.ProductID,
                Size = variation.Size,
                Color = variation.Color,
                Price = variation.Price,
                StockLevel = variation.StockLevel,
                LowStockThreshold = variation.LowStockThreshold,
                ImageUrl = variation.ImageUrl,
                IsActive = variation.IsActive,
                CreatedDate = variation.CreatedDate
            };

            return CreatedAtAction(nameof(GetVariation), new { id = variation.VariationID }, result);
        }

        // PUT: api/variations/{id}
        [HttpPut("~/api/variations/{id}")]
        [Authorize(Roles = "Super Admin,Inventory Clerk")]
        public async Task<IActionResult> PutVariation(int id, UpdateVariationDto dto)
        {
            if (id != dto.VariationID)
                return BadRequest(new { error = "ID mismatch" });

            var variation = await _context.ProductVariations.FindAsync(id);
            if (variation == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            variation.Size = dto.Size;
            variation.Color = dto.Color;
            variation.Price = dto.Price;
            variation.StockLevel = dto.StockLevel;
            variation.LowStockThreshold = dto.LowStockThreshold;
            variation.ImageUrl = dto.ImageUrl;
            variation.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                user.Id,
                $"Updated variation via API: {variation.VariationID}",
                "API",
                $"Product ID: {variation.ProductID}"
            );

            return NoContent();
        }

        // DELETE: api/variations/{id}
        [HttpDelete("~/api/variations/{id}")]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> DeleteVariation(int id)
        {
            var variation = await _context.ProductVariations.FindAsync(id);
            if (variation == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            variation.IsActive = false;
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                user.Id,
                $"Deactivated variation via API: {variation.VariationID}",
                "API",
                $"Product ID: {variation.ProductID}"
            );

            return NoContent();
        }
    }
}