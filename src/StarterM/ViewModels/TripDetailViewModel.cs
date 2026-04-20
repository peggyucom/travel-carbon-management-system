namespace StarterM.ViewModels
{
    public class TripDetailViewModel
    {
        public int DailyTripId { get; set; }
        public int? ApplicationId { get; set; }
        public DateTime Date { get; set; }
        public string TripReason { get; set; } = string.Empty;
        public List<TripExpenseItemViewModel> Expenses { get; set; } = new();
        public bool CanEditTrip { get; set; }
        public bool IsSnapshotView { get; set; }
        public DateTime? SnapshotCreatedAt { get; set; }
        public string ReturnAction { get; set; } = "Details";
        public int TotalAmount => Expenses.Sum(e => e.Amount);

        // 總公里數（以 0 為預設）
        public decimal TotalDistanceKm => Expenses.Sum(e => e.DistanceKm ?? 0m);

        // 若任何項目有預估碳排則回傳合計，否則回傳 null
        public decimal? TotalEstimatedCo2 => Expenses.Any(e => e.EstimatedCo2.HasValue)
            ? Expenses.Sum(e => e.EstimatedCo2 ?? 0m)
            : (decimal?)null;

        // 是否有任何一筆有公里數資料
        public bool HasAnyDistance => Expenses.Any(e => e.DistanceKm.HasValue);
    }

    public class TripExpenseItemViewModel
    {
        public int? ExpenseId { get; set; }
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