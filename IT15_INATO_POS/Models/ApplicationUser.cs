using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLoginDate { get; set; }

        public DateTime? PasswordLastChanged { get; set; }

        // Store password history as JSON to prevent reuse
        public string? PasswordHistory { get; set; } = "[]";

        // Navigation Properties
        public virtual ICollection<Transaction>? Transactions { get; set; }
        public virtual ICollection<AuditLog>? AuditLogs { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
    }
}