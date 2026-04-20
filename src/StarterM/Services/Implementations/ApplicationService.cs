using Microsoft.EntityFrameworkCore;
using StarterM.Data;
using StarterM.Models;
using StarterM.Models.Enums;
using StarterM.Services.Interfaces;
using System.Text.Json;

namespace StarterM.Services.Implementations
{
    public class ApplicationService : IApplicationService
    {
        private const string RejectedSnapshotType = "Rejected";
        private readonly ApplicationDbContext _db;

        public ApplicationService(ApplicationDbContext db)
        {
            _db = db;
        }

        // 把某個員工勾選的多筆 DailyTrip，組成一張新的 Application 申請單。
        public async Task<Application> CreateFromTripsAsync(string employeeId, List<int> tripIds)
        {
            // 1.找出可被建立申請單的 daily trips
            var dailyTrips = await _db.DailyTrips
                .Where(d => d.EmployeeId == employeeId // 抓員工自己的差旅
                    && tripIds.Contains(d.Id)  // 抓勾選的那些
                    && d.ApplicationId == null) // 抓還沒被綁到申請單的差旅
                .Include(d => d.Expenses) //查 daily trip 的同時，把底下的 Expenses 一起載入。
                .OrderBy(d => d.Date)
                .ToListAsync();

            // 2.沒有資料就丟例外
            if (!dailyTrips.Any())//找到一筆就停（快)，相較於 Count 更有效率
                throw new InvalidOperationException("未找到可提交的差旅紀錄"); //目前的操作在當前狀態下不合法(沒資料硬要操作的例外類型)

            // 3.檢查金額是否合法
            ValidateTripsForCreation(dailyTrips);

            var draftStatusId = await GetStatusIdAsync("Draft");
            var departmentId = await ResolveEmployeeDepartmentIdAsync(employeeId);

            // 4.建立一張 Draft 狀態的申請單
            var application = new Application
            {
                EmployeeId = employeeId,
                DepartmentId = departmentId,
                StatusId = draftStatusId,
                CreatedAt = DateTime.UtcNow //UtcNow世界標準時間:資料一致（DB 統一）
            };

            //因為 application.Id 通常是資料庫產生的。要先存，後面才能把各個 trip 的 ApplicationId 指到它。
            _db.Applications.Add(application);
            await _db.SaveChangesAsync();

            // 5.差旅及其費用資料都關聯到這張申請單
            // 差旅掛到申請單.差旅帶上部門.每筆費用也帶上部門
            foreach (var trip in dailyTrips)
            {
                trip.ApplicationId = application.Id;
                trip.DepartmentId = departmentId;

                foreach (var expense in trip.Expenses)
                {
                    expense.DepartmentId = departmentId;
                }
            }
            await _db.SaveChangesAsync();

            return application;
        }


        // 查一張申請單的完整詳細資料。
        public async Task<Application?> GetByIdAsync(int id)
        {
            return await _db.Applications
                .Include(a => a.Department)
                .Include(a => a.Employee)
                .Include(a => a.Status)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses).ThenInclude(e => e.CarbonEmission)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses).ThenInclude(e => e.ExpenseCategory)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses).ThenInclude(e => e.ExpenseItem)
                .Include(a => a.ApprovalHistories).ThenInclude(h => h.Actor)
                .Include(a => a.Approver)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Application>> GetByEmployeeIdAsync(string employeeId)
        {
            return await _db.Applications
                .Where(a => a.EmployeeId == employeeId)
                .Include(a => a.Status)
                .Include(a => a.Department)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // 把申請單從草稿 / 駁回狀態，正式送出審核。
        // 先確認申請單存在、且是本人操作，並檢查狀態、差旅內容、費用、主管與部門資料是否完整；通過後才會將狀態改為 Submitted，設定送出時間與審核主管，並新增一筆送出歷程。
        public async Task SubmitAsync(int applicationId, string employeeId)
        {
            var app = await _db.Applications
                .Include(a => a.Status)
                .Include(a => a.Employee)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            // 商業邏輯驗證
            if (app == null) throw new InvalidOperationException("找不到申請單");
            if (app.EmployeeId != employeeId) throw new UnauthorizedAccessException("無權操作此申請單");
            if (app.Status.Code != "Draft" && app.Status.Code != "Rejected")
                throw new InvalidOperationException("僅草稿或駁回狀態可送出");
            if (!app.DailyTrips.Any())
                throw new InvalidOperationException("申請單至少需包含一筆每日差旅");
            if (!app.DailyTrips.Any(d => d.Expenses.Any()))
                throw new InvalidOperationException("申請單至少需包含一筆費用紀錄");

            ValidateAmounts(app);

            //?.是null 安全運算子（null-conditional operator）
            // 「如果有 → 拿，如果沒有 → 給我 null」

            // string managerId;

            // if (app.Employee == null)
            // {
            //     managerId = null;
            // }
            // else
            // {
            //     managerId = app.Employee.ManagerId;
            // }

            //這樣寫原因:當 Employee == null 時會直接：NullReferenceException（炸掉）

            //指派誰要來審（預設主管）
            if (string.IsNullOrEmpty(app.Employee?.ManagerId))
                throw new InvalidOperationException("尚未設定申請人的直屬主管，無法送出審核");

            //優先用申請單的部門，如果沒有，就用員工的部門
            var departmentId = app.DepartmentId ?? app.Employee?.DepartmentId;
            if (!departmentId.HasValue)
                throw new InvalidOperationException("尚未設定申請人的部門，無法送出申請");

            // 資料同步:補齊部門資訊,把「申請單、差旅、費用」全部的部門欄位都填一致
            // 因為這三層資料其實是分開存的：申請單/每日差旅/費用每一層都有自己的 DepartmentId
            app.DepartmentId = departmentId.Value;

            foreach (var trip in app.DailyTrips)
            {
                trip.DepartmentId = departmentId.Value;

                foreach (var expense in trip.Expenses)
                {
                    expense.DepartmentId = departmentId.Value;
                }
            }

            //改狀態 + 記送出時間 + 指派審核人
            app.StatusId = await GetStatusIdAsync("Submitted");
            app.SubmittedAt = DateTime.UtcNow;
            app.ApproverId = app.Employee!.ManagerId;

            // 寫入審核歷程
            // (ApprovalAction 是一個 enum，用來記錄審核歷程中的操作類型，例如送出、核准、駁回與作廢)
            // 狀態代表系統當前狀態，通常與流程相關且具備擴展性，因此使用資料表；
            // 而動作代表使用者行為，屬於固定集合，適合使用 enum 進行型別限制。
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                ApplicationId = applicationId,
                Action = ApprovalAction.Submit,
                ActorId = employeeId,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }


        //主管核准申請單。
        public async Task ApproveAsync(int applicationId, string approverId)
        {
            var app = await _db.Applications
                .Include(a => a.Status)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) throw new InvalidOperationException("找不到申請單");
            if (app.Status.Code != "Submitted")
                throw new InvalidOperationException("該申請單無法審核");
            if (!IsResponsibleManager(app, approverId))
                throw new UnauthorizedAccessException("無權審核此申請單");

            app.StatusId = await GetStatusIdAsync("Approved");
            app.ApprovedAt = DateTime.UtcNow;
            app.ApproverId = approverId; //「實際做決定的人是誰」

            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                ApplicationId = applicationId,
                Action = ApprovalAction.Approve,
                ActorId = approverId,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        //駁回(申請單/主管/駁回原因)
        public async Task RejectAsync(int applicationId, string approverId, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new InvalidOperationException("駁回時必須填寫原因");

            // 查完整資料
            var app = await _db.Applications
                .Include(a => a.Status)
                .Include(a => a.Employee)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses).ThenInclude(e => e.CarbonEmission)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses).ThenInclude(e => e.ExpenseCategory)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses).ThenInclude(e => e.ExpenseItem)
                .Include(a => a.Approver)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) throw new InvalidOperationException("找不到申請單");
            if (app.Status.Code != "Submitted")
                throw new InvalidOperationException("該申請單無法駁回");
            if (!IsResponsibleManager(app, approverId))
                throw new UnauthorizedAccessException("無權審核此申請單");

            // 建立駁回快照
            // 當下的申請單內容整理成 ReportSnapshotData,再序列化成 JSON 存進 SnapshotJson
            _db.ReportSnapshots.Add(new ReportSnapshot
            {
                ApplicationId = applicationId,
                SnapshotType = RejectedSnapshotType,
                SnapshotJson = JsonSerializer.Serialize(BuildSnapshotData(app)),
                Comment = comment,
                CreatedById = approverId,
                CreatedAt = DateTime.UtcNow
            });

            //把狀態改成 Rejected
            app.StatusId = await GetStatusIdAsync("Rejected");
            app.ApproverId = approverId; //「實際做決定的人是誰」

            //再新增歷程
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                ApplicationId = applicationId,
                Action = ApprovalAction.Reject,
                ActorId = approverId,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        //員工把自己的草稿 / 駁回申請單作廢。
        public async Task VoidAsync(int applicationId, string employeeId)
        {
            var app = await _db.Applications
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null)
                throw new InvalidOperationException("找不到申請單");
            if (app.EmployeeId != employeeId)
                throw new UnauthorizedAccessException("無權操作此申請單");
            if (app.Status.Code != "Draft" && app.Status.Code != "Rejected")
                throw new InvalidOperationException("僅草稿或駁回狀態可作廢");

            //models定義的VoidedFromStatusCode,記錄這張單是從哪個狀態被作廢的(保留狀態轉換歷史的關鍵資訊)
            app.VoidedFromStatusCode = app.Status.Code;
            app.StatusId = await GetStatusIdAsync("Voided");

            //再寫一筆歷程
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                ApplicationId = applicationId,
                Action = ApprovalAction.Void,
                ActorId = employeeId,
                Comment = "申請單已作廢",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        //抓某張申請單最新一筆駁回快照
        public async Task<ReportSnapshot?> GetLatestRejectedSnapshotAsync(int applicationId)
        {
            return await _db.ReportSnapshots
                .Where(s => s.ApplicationId == applicationId && s.SnapshotType == RejectedSnapshotType)
                .Include(s => s.CreatedBy)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        //主管端待審核列表
        public async Task<List<Application>> GetPendingReviewsAsync(string managerId)
        {
            var submittedId = await GetStatusIdAsync("Submitted");
            return await _db.Applications
                .Where(a => a.StatusId == submittedId && a.ApproverId == managerId)
                .Include(a => a.Employee)
                .Include(a => a.Status)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses)
                .OrderBy(a => a.SubmittedAt)
                .ToListAsync();
        }

        //主管端待審核數量
        public async Task<int> GetPendingReviewCountAsync(string managerId)
        {
            var submittedId = await GetStatusIdAsync("Submitted");
            return await _db.Applications.CountAsync(a => a.StatusId == submittedId && a.ApproverId == managerId);
        }

        // 主管審核紀錄
        public async Task<List<Application>> GetAuditHistoryAsync(string managerId)
        {
            //先定義要排除的狀態
            var excludeStatusCodes = new[] { "Draft", "Submitted" };
            return await _db.Applications
                .Include(a => a.Status)
                .Include(a => a.Employee)
                .Include(a => a.ApprovalHistories).ThenInclude(h => h.Actor)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses)
                .Where(a => !excludeStatusCodes.Contains(a.Status.Code) //只留下狀態不是 Draft 或 Submitted 的申請單
                    && a.ApproverId == managerId //主管自己的審核歷史
                    && !(a.Status.Code == "Voided" && a.VoidedFromStatusCode == "Draft")) //排除從 Draft 直接作廢的（因為從未送出過）
                .OrderByDescending(a => a.ApprovedAt ?? a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Application>> GetRejectedByEmployeeAsync(string employeeId)
        {
            var rejectedId = await GetStatusIdAsync("Rejected");
            return await _db.Applications
                .Where(a => a.EmployeeId == employeeId && a.StatusId == rejectedId)
                .Include(a => a.Status)
                .Include(a => a.DailyTrips).ThenInclude(d => d.Expenses)
                .Include(a => a.ApprovalHistories)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // ── helpers ──────────────────────────────────────────────────────

        //抓狀態代碼
        private async Task<int> GetStatusIdAsync(string code)
        {
            var status = await _db.ApplicationStatuses.FirstOrDefaultAsync(s => s.Code == code);
            return status?.Id ?? throw new InvalidOperationException($"找不到狀態代碼: {code}");
        }

        //建立申請單前檢查$0
        private static void ValidateTripsForCreation(IEnumerable<DailyTrip> dailyTrips)
        {
            var tripList = dailyTrips.ToList();
            var zeroAmountTrip = tripList.FirstOrDefault(trip => trip.Expenses.Sum(expense => expense.Amount) == 0);
            if (zeroAmountTrip != null)
                throw new InvalidOperationException($"{zeroAmountTrip.Date:yyyy/MM/dd} 的合計金額為 0，無法產生申請單");

            var totalAmount = tripList.Sum(trip => trip.Expenses.Sum(expense => expense.Amount));
            if (totalAmount == 0)
                throw new InvalidOperationException("勾選差旅的總金額為 0，無法產生申請單");
        }

        //送出申請單前驗證。
        private static void ValidateAmounts(Application app)
        {
            var allExpenses = app.DailyTrips.SelectMany(d => d.Expenses).ToList();
            var totalAmount = allExpenses.Sum(e => e.Amount);
            if (totalAmount == 0)
                throw new InvalidOperationException("申請單總金額為 0，無法送出審核");

            var zeroAmountTrip = app.DailyTrips.FirstOrDefault(trip =>
                trip.Expenses.Sum(expense => expense.Amount) == 0);

            if (zeroAmountTrip != null)
                throw new InvalidOperationException($"{zeroAmountTrip.Date:yyyy/MM/dd} 的合計金額為 0，無法送出審核");
        }

        //這位主管是不是這張單真正該審的人。
        private static bool IsResponsibleManager(Application app, string managerId)
        {
            var responsibleManagerId = app.ApproverId ?? app.Employee?.ManagerId;
            return !string.IsNullOrEmpty(responsibleManagerId) && responsibleManagerId == managerId;
        }

        // 建立申請單前，去users表找這個員工的部門 Id。(只是查詢)
        private async Task<int> ResolveEmployeeDepartmentIdAsync(string employeeId)
        {
            var departmentId = await _db.Users
                .AsNoTracking()  //只查資料，不追蹤變更
                .Where(u => u.Id == employeeId && u.DepartmentId.HasValue)
                .Select(u => u.DepartmentId!.Value)
                .SingleOrDefaultAsync();//預期一個人只有一筆,找不到 → 回傳 0

            if (departmentId <= 0)
                throw new InvalidOperationException("尚未設定申請人的部門，無法建立申請單");

            return departmentId;
        }

        //把申請單整理成「可保存的快照資料結構」
        private static ReportSnapshotData BuildSnapshotData(Application app)
        {
            var dailyTrips = app.DailyTrips
                .OrderBy(d => d.Date)
                .Select(d => new ReportSnapshotDayData
                {
                    DailyTripId = d.Id,
                    Date = d.Date,
                    TripReason = d.TripReason,
                    TransportTotal = d.Expenses.Where(e => e.ExpenseCategory.Code == "DomesticTransport").Sum(e => e.Amount),
                    MealTotal = d.Expenses.Where(e => e.ExpenseCategory.Code == "MealAllowance").Sum(e => e.Amount),
                    LodgingTotal = d.Expenses.Where(e => e.ExpenseCategory.Code == "Lodging").Sum(e => e.Amount),
                    OtherTotal = d.Expenses.Where(e => e.ExpenseCategory.Code == "Other").Sum(e => e.Amount),
                    Expenses = d.Expenses
                        .OrderBy(e => e.CreatedAt)
                        .Select(e => new ReportSnapshotExpenseData
                        {
                            CategoryName = e.ExpenseCategory?.Name ?? string.Empty,
                            ItemName = e.ExpenseItem?.Name ?? string.Empty,
                            Amount = e.Amount,
                            DistanceKm = e.DistanceKm,
                            EstimatedCo2 = e.CarbonEmission?.TotalCo2,
                            Origin = e.Origin,
                            Destination = e.Destination,
                            IsRoundTrip = e.IsRoundTrip,
                            Description = e.Description
                        })
                        .ToList()
                })
                .ToList();

            var allExpenses = app.DailyTrips.SelectMany(d => d.Expenses).ToList();

            return new ReportSnapshotData
            {
                ReportId = app.Id,
                YearMonth = app.DailyTrips.Any() ? app.DailyTrips.Min(d => d.Date).ToString("yyyy-MM") : string.Empty,
                EmployeeName = app.Employee?.Name ?? string.Empty,
                SubmittedAt = app.SubmittedAt,
                ApprovedAt = app.ApprovedAt,
                ApproverName = app.Approver?.Name,
                StartDate = dailyTrips.FirstOrDefault()?.Date ?? default,
                EndDate = dailyTrips.LastOrDefault()?.Date ?? default,
                TotalAmount = dailyTrips.Sum(d => d.TransportTotal + d.MealTotal + d.LodgingTotal + d.OtherTotal),
                TotalCo2 = allExpenses.Where(e => e.CarbonEmission != null).Sum(e => e.CarbonEmission!.TotalCo2),
                DailyTrips = dailyTrips
            };
        }
    }
}
