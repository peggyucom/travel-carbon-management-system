using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    /// <summary>
    /// 費用項目（如：自用車、高鐵、火車、計程車、住宿費、膳雜費、其他費用）
    /// </summary>
    public class ExpenseItem
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public ExpenseCategory Category { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ExpenseItemVehicleTypeMapping? VehicleTypeMapping { get; set; }
    }
}
