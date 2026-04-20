using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    public class VehicleType
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ExpenseItemVehicleTypeMapping> ExpenseItemMappings { get; set; } = new List<ExpenseItemVehicleTypeMapping>();

        public ICollection<EmissionFactor> EmissionFactors { get; set; } = new List<EmissionFactor>();
    }
}