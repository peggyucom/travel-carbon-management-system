using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    /// <summary>
    /// 申請單（取代原本的 MonthlyReport）
    /// </summary>
    public class Application
    {
        public int Id { get; set; }

        [Required]
        public string EmployeeId { get; set; } = string.Empty;
        public ApplicationUser Employee { get; set; } = null!;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int StatusId { get; set; }
        public ApplicationStatus Status { get; set; } = null!;

        /// <summary>作廢前的狀態代碼（Draft 或 Rejected），用於決定顯示邏輯</summary>
        [MaxLength(50)]
        public string? VoidedFromStatusCode { get; set; }

        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public string? ApproverId { get; set; }
        public ApplicationUser? Approver { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<DailyTrip> DailyTrips { get; set; } = new List<DailyTrip>();
        public ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
        public ICollection<ReportSnapshot> Snapshots { get; set; } = new List<ReportSnapshot>();
    }
}
