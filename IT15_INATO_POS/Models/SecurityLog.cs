using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("SecurityLogs")]
    public class SecurityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty; // Login, Logout, FailedLogin, PasswordChange, RoleChange

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}