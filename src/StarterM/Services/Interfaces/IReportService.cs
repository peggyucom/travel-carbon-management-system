namespace StarterM.Services.Interfaces
{
    public interface IReportService
    {
        Task<DashboardKpiData> GetDashboardKpiAsync(string currentUserId, DateTime startDate, DateTime endDate);
        Task<List<TravelCostTrendData>> GetTravelCostTrendAsync(string currentUserId, DateTime startDate, DateTime endDate);
        Task<List<ExpenseCategoryDistributionData>> GetExpenseCategoryDistributionAsync(string currentUserId, DateTime startDate, DateTime endDate);
        Task<List<CostCarbonTrendData>> GetCostCarbonTrendAsync(string currentUserId, DateTime startDate, DateTime endDate);
        Task<List<EmployeeExpenseRankingData>> GetEmployeeExpenseRankingAsync(string currentUserId, DateTime startDate, DateTime endDate, int limit);
        Task<List<EmployeeCarbonRankingData>> GetEmployeeCarbonRankingAsync(string currentUserId, DateTime startDate, DateTime endDate, int limit);
        Task<List<TransportCarbonShareData>> GetTransportCarbonShareAsync(string currentUserId, DateTime startDate, DateTime endDate);
        Task<List<TransportCostCarbonData>> GetTransportCostCarbonAsync(string currentUserId, DateTime startDate, DateTime endDate);
        Task<EmployeeTravelDetailResponse> GetEmployeeTravelDetailAsync(string currentUserId, string employeeId, DateTime startDate, DateTime endDate);
        Task<string?> GetScopedEmployeeNameAsync(string currentUserId, string employeeId);
    }

    public class DashboardKpiData
    {
        public decimal TotalTravelCost { get; set; }
        public decimal TransportationCost { get; set; }
        public decimal TotalCarbonEmission { get; set; }
        public int TravelDays { get; set; }
    }

    public class TravelCostTrendData
    {
        public string Month { get; set; } = string.Empty;
        public decimal TotalTravelCost { get; set; }
    }

    public class ExpenseCategoryDistributionData
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalExpense { get; set; }
        public decimal Percentage { get; set; }
        public int SortOrder { get; set; }
    }

    public class CostCarbonTrendData
    {
        public string Month { get; set; } = string.Empty;
        public decimal TransportationCost { get; set; }
        public decimal CarbonEmission { get; set; }
    }

    public class EmployeeExpenseRankingData
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalExpense { get; set; }
    }

    public class EmployeeCarbonRankingData
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalCarbon { get; set; }
    }

    public class TransportCarbonShareData
    {
        public string TransportType { get; set; } = string.Empty;
        public decimal TotalCarbon { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TransportCostCarbonData
    {
        public string TransportType { get; set; } = string.Empty;
        public decimal TotalExpense { get; set; }
        public decimal TotalCarbon { get; set; }
    }

    public class EmployeeTravelDetailResponse
    {
        public EmployeeTravelSummaryData Summary { get; set; } = new();
        public List<EmployeeTravelDetailData> Items { get; set; } = new();
    }

    public class EmployeeTravelSummaryData
    {
        public decimal TransportationCost { get; set; }
        public decimal TotalCarbon { get; set; }
        public int TravelDays { get; set; }
    }

    public class EmployeeTravelDetailData
    {
        public int? DailyTripId { get; set; }
        public DateTime Date { get; set; }
        public string TransportType { get; set; } = string.Empty;
        public decimal? DistanceKm { get; set; }
        public decimal Amount { get; set; }
        public decimal Carbon { get; set; }
    }
}
