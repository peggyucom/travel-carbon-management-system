using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    /// <summary>
    /// 費用分類（如：國內交通費、住宿費、膳雜費、其他費用）
    /// </summary>
    public class ExpenseCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ExpenseItem> ExpenseItems { get; set; } = new List<ExpenseItem>();
    }
}
