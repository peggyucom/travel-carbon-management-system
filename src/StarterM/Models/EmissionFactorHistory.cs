using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterM.Models
{
    /// <summary>碳排係數歷史紀錄（每次編輯前儲存舊值）</summary>
    public class EmissionFactorHistory
    {
        public int Id { get; set; }

        public int EmissionFactorId { get; set; }

        [MaxLength(50)]
        public string VehicleType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,6)")]
        public decimal Co2PerKm { get; set; }

        public DateTime EffectiveFrom { get; set; }

        [MaxLength(200)]
        public string? Source { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UpdatedById { get; set; }

        public ApplicationUser? UpdatedBy { get; set; }

        public EmissionFactor? EmissionFactor { get; set; }
    }
}
