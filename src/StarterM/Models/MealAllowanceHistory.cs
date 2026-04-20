using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    public class MealAllowanceHistory
    {
        public int Id { get; set; }

        public decimal Rate { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UpdatedById { get; set; }

        public ApplicationUser? UpdatedBy { get; set; }
    }
}
