using StarterM.Models;

namespace StarterM.Services.Interfaces
{
    public interface ICarbonService
    {
        Task<CarbonEmissionRecord?> CalculateAndSaveAsync(ExpenseRecord expense);
        Task<decimal> GetTotalCarbonByEmployeeAsync(string employeeId, int year);
        Task<List<MonthlyCarbonData>> GetMonthlyCarbonAsync(int year);
        Task<List<VehicleCarbonData>> GetCarbonByVehicleTypeAsync(string yearMonth);
    }

    public class MonthlyCarbonData
    {
        public string Month { get; set; } = string.Empty;
        public decimal TotalCo2 { get; set; }
    }

    public class VehicleCarbonData
    {
        public string VehicleType { get; set; } = string.Empty;
        public decimal TotalCo2 { get; set; }
    }
}
