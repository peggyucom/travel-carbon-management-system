using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterM.Models
{
    public class CarbonEmissionRecord
    {
        public int Id { get; set; }

        public int ExpenseId { get; set; }
        public ExpenseRecord Expense { get; set; } = null!;

        [Required, MaxLength(50)]
        public string VehicleType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DistanceKm { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Co2PerKm { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalCo2 { get; set; }

        public int? EmissionFactorId { get; set; }
        public EmissionFactor? EmissionFactor { get; set; }
    }
}
