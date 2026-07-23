using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("SystemLogs")]
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}