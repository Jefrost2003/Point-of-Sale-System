using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("ArchivedUsers")]
    public class ArchivedUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OriginalUserId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? RoleName { get; set; }

        public bool WasActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ArchivedDate { get; set; }

        [Required]
        public string ArchivedBy { get; set; } = string.Empty;

        [ForeignKey("ArchivedBy")]
        public virtual ApplicationUser? ArchivedByUser { get; set; }
    }
}