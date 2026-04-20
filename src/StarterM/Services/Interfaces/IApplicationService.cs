using StarterM.Models;

namespace StarterM.Services.Interfaces
{
    public interface IApplicationService
    {
        Task<Application> CreateFromTripsAsync(string employeeId, List<int> tripIds);
        Task<Application?> GetByIdAsync(int id);
        Task<List<Application>> GetByEmployeeIdAsync(string employeeId);
        // 刪除草稿已移除：請使用 VoidAsync 保留稽核紀錄
        Task SubmitAsync(int applicationId, string employeeId);
        Task ApproveAsync(int applicationId, string approverId);
        Task RejectAsync(int applicationId, string approverId, string comment);
        Task VoidAsync(int applicationId, string employeeId);
        Task<ReportSnapshot?> GetLatestRejectedSnapshotAsync(int applicationId);
        Task<List<Application>> GetPendingReviewsAsync(string managerId);
        Task<int> GetPendingReviewCountAsync(string managerId);
        Task<List<Application>> GetAuditHistoryAsync(string managerId);
        Task<List<Application>> GetRejectedByEmployeeAsync(string employeeId);
    }
}
