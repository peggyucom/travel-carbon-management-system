using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterM.Models
{
    public class ExpenseRecord
    {
        public int Id { get; set; }

        [Required]
        public string EmployeeId { get; set; } = string.Empty;
        public ApplicationUser Employee { get; set; } = null!;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; } = null!;

        // 關聯每日差旅
        public int? DailyTripId { get; set; }
        public DailyTrip? DailyTrip { get; set; }

        [Required]
        public DateTime Date { get; set; }

        // 費用分類（FK → ExpenseCategories）
        [Required]
        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory ExpenseCategory { get; set; } = null!;

        // 費用項目（FK → ExpenseItems）
        [Required]
        public int ExpenseItemId { get; set; }
        public ExpenseItem ExpenseItem { get; set; } = null!;

        [Required]
        public int Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DistanceKm { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // 起點、終點
        [MaxLength(200)]
        public string? Origin { get; set; }

        [MaxLength(200)]
        public string? Destination { get; set; }

        // 是否為往返
        public bool IsRoundTrip { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public CarbonEmissionRecord? CarbonEmission { get; set; }
    }
}
