using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }  // Primary key.

        [Required]
        public string UserID { get; set; } = string.Empty;  // User who performed the action (FK to AspNetUsers).

        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;  // Description of the action (e.g., "Created product").

        [Required]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty;  // System module (e.g., "Products", "POS").

        [StringLength(1000)]
        public string? Details { get; set; }  // Additional details (JSON or plain text).

        [StringLength(50)]
        public string? IpAddress { get; set; }  // Client IP address.

        public DateTime Timestamp { get; set; } = DateTime.Now;  // When the action occurred.

        // Foreign key to user.
        [ForeignKey("UserID")]
        public virtual ApplicationUser? User { get; set; }
    }
}