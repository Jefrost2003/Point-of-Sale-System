using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("SalesItems")]
    public class SalesItem
    {
        [Key]
        public int SalesItemID { get; set; }  // Primary key.

        [Required]
        public int TransID { get; set; }  // Foreign key to Transactions.

        [Required]
        public int VariationID { get; set; }  // Foreign key to ProductVariations.

        [Required]
        public int Quantity { get; set; }  // Number of units sold.

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }  // Price per unit at the time of sale.

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }  // Quantity × UnitPrice.

        public DateTime CreatedDate { get; set; } = DateTime.Now;  // Record creation timestamp.

        [Required]
        public string CreatedBy { get; set; } = string.Empty;  // User ID who created this record (cashier).

        // Foreign keys
        [ForeignKey("TransID")]
        public virtual Transaction? Transaction { get; set; }

        [ForeignKey("VariationID")]
        public virtual ProductVariation? ProductVariation { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Creator { get; set; }
    }
}