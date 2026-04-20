using Microsoft.EntityFrameworkCore;
using StarterM.Data;
using StarterM.Models;
using StarterM.Services.Interfaces;

namespace StarterM.Services.Implementations
{
    public class DailyTripService : IDailyTripService
    {
        private readonly ApplicationDbContext _db;

        public DailyTripService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DailyTrip> CreateAsync(string employeeId, DateTime date, string tripReason, int? applicationId = null)
        {
            // 檢查日期是否重複：同員工同日期，且不屬於已作廢申請單的 DailyTrip
            var existing = await _db.DailyTrips
                .Include(d => d.Application).ThenInclude(a => a!.Status)
                .FirstOrDefaultAsync(d => d.EmployeeId == employeeId
                    && d.Date.Date == date.Date
                    && (d.ApplicationId == null || d.Application!.Status.Code != "Voided"));
            if (existing != null)
                throw new InvalidOperationException("該日期已有差旅紀錄，如需重新建立請先刪除原紀錄");

            int? departmentId = null;

            if (applicationId.HasValue)
            {
                var app = await _db.Applications
                    .Include(a => a.Status)
                    .FirstOrDefaultAsync(a => a.Id == applicationId.Value);

                if (app == null)
                    throw new InvalidOperationException("找不到要加入的申請單");

                if (app.EmployeeId != employeeId)
                    throw new UnauthorizedAccessException("無權操作此申請單");

                if (app.Status.Code != "Draft" && app.Status.Code != "Rejected")
                    throw new InvalidOperationException("僅草稿或已駁回申請單可新增差旅");

                departmentId = app.DepartmentId ?? await ResolveEmployeeDepartmentIdAsync(employeeId);
            }

            departmentId ??= await ResolveEmployeeDepartmentIdAsync(employeeId);

            var trip = new DailyTrip
            {
                EmployeeId = employeeId,
                DepartmentId = departmentId.Value,
                ApplicationId = applicationId,
                Date = date,
                TripReason = tripReason,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.DailyTrips.Add(trip);
            await _db.SaveChangesAsync();
            return trip;
        }

        public async Task<DailyTrip?> GetByIdAsync(int id)
        {
            return await _db.DailyTrips
                .Include(d => d.Employee)
                .Include(d => d.Application).ThenInclude(a => a!.Status)
                .Include(d => d.Expenses).ThenInclude(e => e.CarbonEmission)
                .Include(d => d.Expenses).ThenInclude(e => e.ExpenseCategory)
                .Include(d => d.Expenses).ThenInclude(e => e.ExpenseItem)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<DailyTrip>> GetByEmployeeIdAsync(string employeeId)
        {
            return await _db.DailyTrips
                .Where(d => d.EmployeeId == employeeId)
                .Include(d => d.Application).ThenInclude(a => a!.Status)
                .Include(d => d.Expenses)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<DailyTrip> UpdateAsync(DailyTrip trip)
        {
            var existing = await _db.DailyTrips
                .Include(d => d.Application).ThenInclude(a => a!.Status)
                .FirstOrDefaultAsync(d => d.Id == trip.Id);
            if (existing == null)
                throw new InvalidOperationException("找不到該每日差旅紀錄");

            var appStatus = existing.Application?.Status?.Code;
            if (appStatus != null && appStatus != "Draft" && appStatus != "Rejected")
                throw new InvalidOperationException("此申請單狀態下無法編輯每日差旅");

            existing.Date = trip.Date;
            existing.TripReason = trip.TripReason;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var trip = await _db.DailyTrips
                .Include(d => d.Application).ThenInclude(a => a!.Status)
                .Include(d => d.Expenses)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (trip == null) return false;

            var appStatus = trip.Application?.Status?.Code;
            if (appStatus != null && appStatus != "Draft" && appStatus != "Rejected")
            {
                throw new InvalidOperationException("此差旅所屬申請單已送出，無法刪除");
            }

            _db.DailyTrips.Remove(trip);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<DailyTrip>> GetUnlinkedByEmployeeAsync(string employeeId)
        {
            return await _db.DailyTrips
                .Where(d => d.EmployeeId == employeeId && d.ApplicationId == null)
                .Include(d => d.Expenses)
                .OrderBy(d => d.Date)
                .ToListAsync();
        }

        private async Task<int> ResolveEmployeeDepartmentIdAsync(string employeeId)
        {
            var departmentId = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == employeeId && u.DepartmentId.HasValue)
                .Select(u => u.DepartmentId!.Value)
                .SingleOrDefaultAsync();

            if (departmentId <= 0)
                throw new InvalidOperationException("目前帳號尚未設定部門，無法建立差旅資料。");

            return departmentId;
        }
    }
}
