using Microsoft.EntityFrameworkCore;       // EF Core 查詢方法，例如 Include、ToListAsync()
using StarterM.Data;                       // ApplicationDbContext
using StarterM.Models;                     // Entity 模型，例如 ExpenseRecord
using StarterM.Services.Interfaces;        // Service 介面

namespace StarterM.Services.Implementations
{
    // ExpenseService：負責費用明細的建立、查詢、刪除，
    // 並處理國內交通的驗證與碳排計算。
    //
    // 這層偏向「資料落地前後的業務規則」：
    // - 驗證這筆資料能不能建立
    // - 補齊後端必須控制的欄位
    // - 寫入資料庫
    // - 寫完後觸發碳排計算
    //
    // 注意：距離怎麼 preview、前端怎麼畫地圖，這些不在這層處理。
    public class ExpenseService : IExpenseService
    {
        // 國內交通「合理距離上限」
        // 超過 1000 公里就視為異常資料
        private const decimal MaxDomesticTransportDistanceKm = 1000m;

        // ===== 依賴注入進來的服務 / 物件 =====
        // 資料庫 context，負責查詢與儲存資料
        private readonly ApplicationDbContext _db;

        // 碳排 service，用來在國內交通費建立後自動計算碳排
        private readonly ICarbonService _carbonService;

        // 建構式：由 DI 容器自動把需要的服務注入進來
        public ExpenseService(ApplicationDbContext db, ICarbonService carbonService)
        {
            _db = db;
            _carbonService = carbonService;
        }

        // =========================================================
        // 1. 建立費用明細
        // =========================================================
        public async Task<ExpenseRecord> CreateAsync(ExpenseRecord record)
        {
            // 先查出這筆費用所屬的每日差旅
            // 並把申請單與狀態一起載入，方便做權限與流程檢查
            var trip = await _db.DailyTrips
                .Include(d => d.Application).ThenInclude(a => a!.Status)
                .FirstOrDefaultAsync(d => d.Id == record.DailyTripId);

            // 找不到差旅，代表資料不合法
            if (trip == null)
                throw new InvalidOperationException("找不到對應的每日差旅");

            // 只有這筆差旅的本人，才能新增費用
            if (trip.EmployeeId != record.EmployeeId)
                throw new UnauthorizedAccessException("無權操作此差旅費用");

            // 如果所屬申請單已進到送審之後的狀態，就不允許再新增費用
            if (trip.Application != null
                && trip.Application.Status.Code != "Draft"
                && trip.Application.Status.Code != "Rejected")
            {
                throw new InvalidOperationException("此差旅所屬申請單已送出，無法新增費用");
            }

            // 部門 ID 來源優先順序：
            // 1. 申請單上的 DepartmentId
            // 2. 每日差旅上的 DepartmentId
            // 3. 員工帳號上的 DepartmentId
            var departmentId = trip.Application?.DepartmentId ?? trip.DepartmentId ?? await ResolveEmployeeDepartmentIdAsync(record.EmployeeId);

            // 後端統一補齊 / 覆蓋費用資料，避免前端送錯。
            // 例如：日期應該跟 DailyTrip 同步，而不是信任前端任意送值。
            record.Date = trip.Date;
            record.DepartmentId = departmentId;
            record.CreatedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;

            // 如果差旅本身還沒補到部門，也一起補上
            if (!trip.DepartmentId.HasValue)
            {
                trip.DepartmentId = departmentId;
            }

            // 如果申請單存在，且申請單還沒補到部門，也一起補上
            if (trip.Application != null && !trip.Application.DepartmentId.HasValue)
            {
                trip.Application.DepartmentId = departmentId;
            }

            // 國內交通費要先驗證公里數是否合法。
            // 先驗證再存檔，可以避免錯誤資料先落地。
            await ValidateDomesticTransportDistanceAsync(record);

            // 寫入費用明細
            _db.ExpenseRecords.Add(record);
            await _db.SaveChangesAsync();

            // 國內交通費自動計算碳排。
            // 放在存檔後處理，是因為碳排紀錄通常依附於已存在的費用主檔。
            var category = await _db.ExpenseCategories.FindAsync(record.ExpenseCategoryId);
            if (category?.Code == "DomesticTransport"
                && record.DistanceKm.HasValue && record.DistanceKm > 0)
            {
                await _carbonService.CalculateAndSaveAsync(record);
            }

            return record;
        }

        // =========================================================
        // 2. 依費用 ID 取得單筆費用明細
        // =========================================================
        public async Task<ExpenseRecord?> GetByIdAsync(int id)
        {
            return await _db.ExpenseRecords
                .Include(e => e.Employee)
                .Include(e => e.CarbonEmission)
                .Include(e => e.DailyTrip).ThenInclude(d => d!.Application).ThenInclude(a => a!.Status)
                .Include(e => e.ExpenseCategory)
                .Include(e => e.ExpenseItem)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        // =========================================================
        // 3. 依員工 ID 取得該員工所有費用明細
        // =========================================================
        public async Task<List<ExpenseRecord>> GetByEmployeeIdAsync(string employeeId)
        {
            return await _db.ExpenseRecords
                .Where(e => e.EmployeeId == employeeId)
                .Include(e => e.DailyTrip)
                .Include(e => e.ExpenseCategory)
                .Include(e => e.ExpenseItem)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        // =========================================================
        // 4. 刪除費用明細
        // =========================================================
        public async Task<bool> DeleteAsync(int id)
        {
            // 查這筆費用，並把所屬差旅 / 申請單狀態一起載入
            var record = await _db.ExpenseRecords
                .Include(e => e.DailyTrip).ThenInclude(d => d!.Application).ThenInclude(a => a!.Status)
                .FirstOrDefaultAsync(e => e.Id == id);

            // 找不到就回傳 false
            if (record == null) return false;

            // 若申請單已送審完成或進入其他不可修改狀態，就不能刪
            var appStatus = record.DailyTrip?.Application?.Status?.Code;
            if (appStatus != null && appStatus != "Draft" && appStatus != "Rejected")
                throw new InvalidOperationException("此申請單狀態下無法刪除費用");

            // 執行刪除
            _db.ExpenseRecords.Remove(record);
            await _db.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // 5. 依申請單 ID 取得底下所有費用明細
        // =========================================================
        public async Task<List<ExpenseRecord>> GetByApplicationIdAsync(int applicationId)
        {
            return await _db.ExpenseRecords
                .Where(e => e.DailyTrip != null && e.DailyTrip.ApplicationId == applicationId)
                .Include(e => e.CarbonEmission)
                .Include(e => e.DailyTrip)
                .Include(e => e.ExpenseCategory)
                .Include(e => e.ExpenseItem)
                .OrderBy(e => e.Date)
                .ToListAsync();
        }

        // =========================================================
        // 6. 依每日差旅 ID 取得該日所有費用明細
        // =========================================================
        public async Task<List<ExpenseRecord>> GetByDailyTripIdAsync(int dailyTripId)
        {
            return await _db.ExpenseRecords
                .Where(e => e.DailyTripId == dailyTripId)
                .Include(e => e.CarbonEmission)
                .Include(e => e.ExpenseCategory)
                .Include(e => e.ExpenseItem)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();
        }

        // =========================================================
        // 7. 驗證國內交通費的距離是否合法
        // =========================================================
        private async Task ValidateDomesticTransportDistanceAsync(ExpenseRecord record)
        {
            // 先確認這筆費用是不是國內交通
            var category = await _db.ExpenseCategories.FindAsync(record.ExpenseCategoryId);
            if (category?.Code != "DomesticTransport")
                return;

            // 國內交通一定要有公里數，因為後面要算碳排。
            // 這層只驗證結果，不重新計算距離；
            // 距離來源由 controller / distance service 決定。
            if (!record.DistanceKm.HasValue || record.DistanceKm.Value <= 0)
                throw new InvalidOperationException("國內交通費必須填寫公里數，才能計算碳排。");

            // 超過合理上限也視為異常
            if (record.DistanceKm.Value > MaxDomesticTransportDistanceKm)
                throw new InvalidOperationException("距離異常：單日往返距離超過 1000 公里，請重新定位起訖點並再次計算。");
        }

        // =========================================================
        // 8. 取得員工帳號所屬部門 ID
        //    若帳號沒綁部門，就不允許建立費用資料
        // =========================================================
        private async Task<int> ResolveEmployeeDepartmentIdAsync(string employeeId)
        {
            // 建立費用資料時一定要能追溯到部門，
            // 否則後續統計、簽核、報表都會缺資料。
            var departmentId = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == employeeId && u.DepartmentId.HasValue)
                .Select(u => u.DepartmentId!.Value)
                .SingleOrDefaultAsync();

            if (departmentId <= 0)
                throw new InvalidOperationException("目前帳號尚未設定部門，無法建立費用資料。");

            return departmentId;
        }
    }
}
