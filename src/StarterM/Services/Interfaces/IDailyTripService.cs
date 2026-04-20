using StarterM.Models;

namespace StarterM.Services.Interfaces
{
    public interface IDailyTripService
    {
        Task<DailyTrip> CreateAsync(string employeeId, DateTime date, string tripReason, int? applicationId = null);
        Task<DailyTrip?> GetByIdAsync(int id);
        Task<List<DailyTrip>> GetByEmployeeIdAsync(string employeeId);
        Task<DailyTrip> UpdateAsync(DailyTrip trip);
        Task<bool> DeleteAsync(int id);
        Task<List<DailyTrip>> GetUnlinkedByEmployeeAsync(string employeeId);
    }
}
