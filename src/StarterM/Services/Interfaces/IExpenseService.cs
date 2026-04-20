using StarterM.Models;

namespace StarterM.Services.Interfaces
{
    public interface IExpenseService
    {
        Task<ExpenseRecord> CreateAsync(ExpenseRecord record);
        Task<ExpenseRecord?> GetByIdAsync(int id);
        Task<List<ExpenseRecord>> GetByEmployeeIdAsync(string employeeId);
        Task<bool> DeleteAsync(int id);
        Task<List<ExpenseRecord>> GetByApplicationIdAsync(int applicationId);
        Task<List<ExpenseRecord>> GetByDailyTripIdAsync(int dailyTripId);
    }
}
