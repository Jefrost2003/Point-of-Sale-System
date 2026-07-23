using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("ProductVariations")]
    public class ProductVariation
    {
        [Key]
        public int VariationID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(20)]
        public string Size { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Color { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        public int StockLevel { get; set; } = 0;

        public int LowStockThreshold { get; set; } = 10;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [NotMapped]
        public bool IsLowStock => StockLevel <= LowStockThreshold;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<SalesItem>? SalesItems { get; set; }
    }
}