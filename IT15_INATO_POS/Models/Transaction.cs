using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("Transactions")]
    public class Transaction
    {
        [Key]
        public int TransID { get; set; }  // Primary key.

        [Required]
        public string CashierID { get; set; } = string.Empty;  // User ID of the cashier (FK to AspNetUsers).

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }  // Sum of all line items, stored with 2 decimals.

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }  // Amount tendered by customer.

        [Column(TypeName = "decimal(10,2)")]
        public decimal ChangeAmount { get; set; }  // Change returned to customer.

        [StringLength(100)]
        public string? PayMongoReference { get; set; }  // Payment gateway reference ID (for GCash/Card).

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";  // e.g., "Cash", "GCash", "Card".

        [StringLength(50)]
        public string TransactionStatus { get; set; } = "Completed";  // e.g., "Completed", "Refunded".

        public DateTime TransactionDate { get; set; } = DateTime.Now;  // Date and time of transaction.

        [StringLength(500)]
        public string? Notes { get; set; }  // Optional notes.

        // Foreign key to cashier.
        [ForeignKey("CashierID")]
        public virtual ApplicationUser? Cashier { get; set; }

        // Navigation property: one transaction has many line items.
        public virtual ICollection<SalesItem>? SalesItems { get; set; }
    }
}