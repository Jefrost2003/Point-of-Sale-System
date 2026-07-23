using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    // ASPNET_MONSTERASP: This table stores active product information on the MonsterASP database.
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }  // Primary key, auto‑incremented.

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;  // Product name, e.g., "Basic T‑Shirt".

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;  // Stock Keeping Unit, must be unique.

        [StringLength(500)]
        public string? Description { get; set; }  // Optional product description.

        [StringLength(500)]
        public string? ImageUrl { get; set; }  // Absolute URL to the main product image.

        public bool IsActive { get; set; } = true;  // Soft delete flag: false means archived (moved to Archive table).

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.Now;  // Record creation timestamp.

        [Required]
        public string CreatedBy { get; set; } = string.Empty;  // User ID of the creator (FK to AspNetUsers).

        public DateTime? ModifiedDate { get; set; }  // Last modification timestamp.

        public string? ModifiedBy { get; set; }  // User ID of the last modifier.

        // Foreign key relationships
        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Creator { get; set; }  // Navigation property to the user who created this product.

        // Navigation property: one product can have many variations (size/color combinations).
        public virtual ICollection<ProductVariation>? ProductVariations { get; set; }
    }
}