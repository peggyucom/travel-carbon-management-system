using StarterM.Models;

namespace StarterM.ViewModels
{
    /// <summary>
    /// 申請單明細 ViewModel — 按日分組顯示
    /// </summary>
    public class ApplicationDetailViewModel
    {
        public int ApplicationId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApproverName { get; set; }
        public string? VoidedFromStatusCode { get; set; }
        public bool IsSnapshotView { get; set; }
        public DateTime? SnapshotCreatedAt { get; set; }
        public bool HasRejectedSnapshot { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DailyTripSummary> DailyTrips { get; set; } = new();
        public int TotalAmount { get; set; }
        public decimal TotalCo2 { get; set; }
        public List<ApprovalHistoryItem> ApprovalHistories { get; set; } = new();
    }

    public class DailyTripSummary
    {
        public int DailyTripId { get; set; }
        public DateTime Date { get; set; }
        public string TripReason { get; set; } = string.Empty;
        public int TransportTotal { get; set; }
        public int MealTotal { get; set; }
        public int LodgingTotal { get; set; }
        public int OtherTotal { get; set; }
        public int DayTotal => TransportTotal + MealTotal + LodgingTotal + OtherTotal;
        public List<ExpenseRecord> Expenses { get; set; } = new();
    }

    public class ApprovalHistoryItem
    {
        public DateTime CreatedAt { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? ActorName { get; set; }
        public string? Comment { get; set; }
    }
}
