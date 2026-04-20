using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    public class CarAllowanceHistory
    {
        public int Id { get; set; }

        /// <summary>每公里補助金額（元）</summary>
        public decimal RatePerKm { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UpdatedById { get; set; }

        public ApplicationUser? UpdatedBy { get; set; }
    }
}
