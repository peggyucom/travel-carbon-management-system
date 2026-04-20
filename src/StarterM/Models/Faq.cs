using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    public class Faq
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "報支流程";

        public bool IsActive { get; set; } = true;
    }
}
