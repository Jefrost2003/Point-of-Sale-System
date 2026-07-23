using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_INATO_POS.Models
{
    [Table("Archive")]
    public class Archive
    {
        [Key]
        public int ArchiveID { get; set; }

        [Required]
        public int OriginalProductID { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int VariationCount { get; set; }

        public DateTime ArchivedDate { get; set; }

        [Required]
        public string ArchivedBy { get; set; } = string.Empty;

        [ForeignKey("ArchivedBy")]
        public virtual ApplicationUser? ArchivedByUser { get; set; }
    }
}