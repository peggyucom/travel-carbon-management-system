using StarterM.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    public class ApprovalHistory
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        [Required]
        public ApprovalAction Action { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        [Required]
        public string ActorId { get; set; } = string.Empty;
        public ApplicationUser Actor { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
