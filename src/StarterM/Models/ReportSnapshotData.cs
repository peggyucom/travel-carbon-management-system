namespace StarterM.Models
{
    public class ReportSnapshotData
    {
        public int ReportId { get; set; }
        public string YearMonth { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApproverName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalAmount { get; set; }
        public decimal TotalCo2 { get; set; }
        public List<ReportSnapshotDayData> DailyTrips { get; set; } = new();
    }

    public class ReportSnapshotDayData
    {
        public int DailyTripId { get; set; }
        public DateTime Date { get; set; }
        public string TripReason { get; set; } = string.Empty;
        public int TransportTotal { get; set; }
        public int MealTotal { get; set; }
        public int LodgingTotal { get; set; }
        public int OtherTotal { get; set; }
        public List<ReportSnapshotExpenseData> Expenses { get; set; } = new();
    }

    public class ReportSnapshotExpenseData
    {
        public string CategoryName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Amount { get; set; }
        public decimal? DistanceKm { get; set; }
        public decimal? EstimatedCo2 { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public bool IsRoundTrip { get; set; }
        public string? Description { get; set; }
    }
}