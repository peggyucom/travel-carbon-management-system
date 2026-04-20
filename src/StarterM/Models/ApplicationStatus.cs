using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    /// <summary>
    /// 申請單狀態（Draft草稿、Submitted送出、Approved核准、Rejected駁回、Voided作廢）
    /// </summary>
    public class ApplicationStatus
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
