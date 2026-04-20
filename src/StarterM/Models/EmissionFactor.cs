using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterM.Models
{
    public class EmissionFactor
    {
        public int Id { get; set; }

        public int VehicleTypeId { get; set; }
        public VehicleType VehicleType { get; set; } = null!;

        [Column(TypeName = "decimal(18,6)")]
        public decimal Co2PerKm { get; set; }

        public DateTime EffectiveFrom { get; set; }

        [MaxLength(200)]
        public string? Source { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? UpdatedById { get; set; }

        public ApplicationUser? UpdatedBy { get; set; }
    }
}
