using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    /// <summary>
    /// 每日差旅：員工每日出差的基本資訊，底下可新增多筆費用明細
    /// </summary>
    public class DailyTrip
    {
        public int Id { get; set; }

        [Required]
        public string EmployeeId { get; set; } = string.Empty;
        public ApplicationUser Employee { get; set; } = null!;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? ApplicationId { get; set; }
        public Application? Application { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required, MaxLength(200)]
        public string TripReason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ExpenseRecord> Expenses { get; set; } = new List<ExpenseRecord>();
    }
}
