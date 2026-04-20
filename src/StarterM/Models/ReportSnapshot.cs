using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    public class ReportSnapshot
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        [Required, MaxLength(50)]
        public string SnapshotType { get; set; } = string.Empty;

        [Required]
        public string SnapshotJson { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Comment { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;
        public ApplicationUser CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}