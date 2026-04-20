using Microsoft.AspNetCore.Authorization;   // [Authorize] 權限控管屬性
using Microsoft.AspNetCore.Identity;        // UserManager，用來取得目前登入者
using Microsoft.AspNetCore.Mvc;             // Controller、IActionResult 等 MVC 核心類別
using StarterM.Models;                      // Entity 模型，例如 Application、ApplicationUser
using StarterM.Services.Interfaces;         // Service 介面
using StarterM.ViewModels;                  // ViewModel
using System.Text.Json;                     // JSON 反序列化，用來還原快照資料

namespace StarterM.Controllers
{
    // 整個 Controller 都需要登入後才能使用
    [Authorize]
    public class ApplicationController : Controller
    {
        // ===== 依賴注入進來的服務 / 物件 =====
        // 申請單相關 service
        private readonly IApplicationService _applicationService;

        // Identity 的 UserManager，用來取得目前登入使用者
        private readonly UserManager<ApplicationUser> _userManager;

        // 建構式：由 DI 容器自動把需要的服務注入進來
        public ApplicationController(IApplicationService applicationService, UserManager<ApplicationUser> userManager)
        {
            _applicationService = applicationService;
            _userManager = userManager;
        }

        // =========================================================
        // 1. 申請單列表頁
        //    員工看自己的申請單
        //    主管另外會看到待審核清單
        // =========================================================
        public async Task<IActionResult> Index()
        {
            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            // 如果目前身分是主管，就另外查待審核申請單
            if (User.IsInRole("Manager"))
            {
                var pendingReviews = await _applicationService.GetPendingReviewsAsync(userId);
                ViewData["PendingReviews"] = pendingReviews;
            }

            // 查出目前員工自己的申請單列表
            var myApplications = await _applicationService.GetByEmployeeIdAsync(userId);

            // 把資料傳給 View 顯示
            return View(myApplications);
        }

        // =========================================================
        // 2. 從勾選的差旅建立申請單（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromTrips(List<int> selectedTripIds)
        {
            // 至少要選一筆差旅，才能建立申請單
            if (selectedTripIds == null || !selectedTripIds.Any())
            {
                TempData["Error"] = "請至少選擇一筆差旅";
                return RedirectToAction("Index", "Expense");
            }

            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // 呼叫 service，將這些差旅組成一張申請單
                var app = await _applicationService.CreateFromTripsAsync(userId, selectedTripIds);

                TempData["Success"] = "申請單已建立";

                // 建立完成後導向申請單詳細頁
                return RedirectToAction(nameof(Details), new { id = app.Id });
            }
            catch (InvalidOperationException ex)
            {
                // 若 service 判定不符合建立條件，就回到差旅列表並顯示錯誤
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Expense");
            }
        }

        // =========================================================
        // 3. 申請單詳細頁
        //    本人可看，主管僅能看自己負責審核的申請單
        // =========================================================
        public async Task<IActionResult> Details(int id)
        {
            // 先查申請單是否存在
            var app = await _applicationService.GetByIdAsync(id);
            if (app == null) return NotFound();

            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            // 若不是本人，也不是負責此申請單的主管，就禁止存取
            if (app.EmployeeId != userId && !CanAccessAsResponsibleManager(app, userId))
                return Forbid();

            // 先建立「目前最新資料」版本的詳細 ViewModel
            var vm = BuildCurrentDetailViewModel(app);

            // 主管看「已駁回」申請單時，要顯示駁回當下的快照資料
            if (User.IsInRole("Manager") && app.Status.Code == "Rejected")
            {
                var snapshot = await _applicationService.GetLatestRejectedSnapshotAsync(app.Id);
                if (snapshot != null)
                {
                    // 把快照 JSON 還原成物件
                    var snapshotData = JsonSerializer.Deserialize<ReportSnapshotData>(snapshot.SnapshotJson);
                    if (snapshotData != null)
                    {
                        // 以快照資料重建詳細畫面
                        vm = BuildSnapshotDetailViewModel(app, snapshotData, snapshot.CreatedAt);
                    }
                }
            }

            // 作廢申請單的額外顯示邏輯
            if (app.Status.Code == "Voided")
            {
                // 記錄它原本是從哪個狀態被作廢
                vm.VoidedFromStatusCode = app.VoidedFromStatusCode;

                // 如果是由 Rejected 作廢，畫面上可提供查看駁回快照的入口
                if (app.VoidedFromStatusCode == "Rejected")
                {
                    var snapshot = await _applicationService.GetLatestRejectedSnapshotAsync(app.Id);
                    vm.HasRejectedSnapshot = snapshot != null;
                }
            }

            return View(vm);
        }

        // =========================================================
        // 4. 查看駁回快照
        //    可供 AJAX 或獨立頁面使用
        // =========================================================
        public async Task<IActionResult> ViewSnapshot(int id)
        {
            // 先查申請單
            var app = await _applicationService.GetByIdAsync(id);
            if (app == null) return NotFound();

            // 權限檢查：本人或負責主管才能看
            var userId = _userManager.GetUserId(User)!;
            if (app.EmployeeId != userId && !CanAccessAsResponsibleManager(app, userId))
                return Forbid();

            // 查最近一次駁回時留下的快照
            var snapshot = await _applicationService.GetLatestRejectedSnapshotAsync(id);
            if (snapshot == null) return NotFound();

            // 將 JSON 快照內容反序列化
            var snapshotData = JsonSerializer.Deserialize<ReportSnapshotData>(snapshot.SnapshotJson);
            if (snapshotData == null) return NotFound();

            // 以快照資料建立 ViewModel，沿用 Details 畫面顯示
            var vm = BuildSnapshotDetailViewModel(app, snapshotData, snapshot.CreatedAt);
            return View("Details", vm);
        }

        // =========================================================
        // 5. 查看快照中的單日差旅明細
        // =========================================================
        public async Task<IActionResult> TripSnapshot(int id, int dailyTripId)
        {
            // 先查申請單
            var app = await _applicationService.GetByIdAsync(id);
            if (app == null) return NotFound();

            // 權限檢查：本人或負責主管才能看
            var userId = _userManager.GetUserId(User)!;
            if (app.EmployeeId != userId && !CanAccessAsResponsibleManager(app, userId))
                return Forbid();

            // 查最近一次駁回快照
            var snapshot = await _applicationService.GetLatestRejectedSnapshotAsync(id);
            if (snapshot == null) return NotFound();

            // 將 JSON 快照內容反序列化
            var snapshotData = JsonSerializer.Deserialize<ReportSnapshotData>(snapshot.SnapshotJson);
            if (snapshotData == null) return NotFound();

            // 在快照裡找到指定的單日差旅
            var day = snapshotData.DailyTrips.FirstOrDefault(d => d.DailyTripId == dailyTripId);
            if (day == null) return NotFound();

            // 把快照中的差旅資料轉成 TripDetails 頁面可用的 ViewModel
            var vm = new TripDetailViewModel
            {
                DailyTripId = day.DailyTripId,
                ApplicationId = app.Id,
                Date = day.Date,
                TripReason = day.TripReason,
                CanEditTrip = false,
                IsSnapshotView = true,
                SnapshotCreatedAt = snapshot.CreatedAt,

                // 如果申請單已作廢，返回按鈕要回快照頁；否則回一般詳細頁
                ReturnAction = app.Status.Code == "Voided" ? "ViewSnapshot" : "Details",

                // 把快照中的費用明細轉成前端用的 ViewModel
                Expenses = day.Expenses
                    .Select(e => new TripExpenseItemViewModel
                    {
                        CategoryName = string.IsNullOrEmpty(e.CategoryName) ? "-" : e.CategoryName,
                        ItemName = string.IsNullOrEmpty(e.ItemName) ? "-" : e.ItemName,
                        Amount = e.Amount,
                        DistanceKm = e.DistanceKm,
                        EstimatedCo2 = e.EstimatedCo2,
                        Origin = e.Origin,
                        Destination = e.Destination,
                        IsRoundTrip = e.IsRoundTrip,
                        Description = e.Description
                    })
                    .ToList()
            };

            // 直接沿用 Expense/TripDetails.cshtml 顯示差旅快照
            return View("~/Views/Expense/TripDetails.cshtml", vm);
        }

        // 已移除刪除動作：請改用作廢 (VoidApplication)

        // =========================================================
        // 6. 送出申請單審核（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // 呼叫 service 將申請單狀態改為已送審
                await _applicationService.SubmitAsync(id, userId);
                TempData["Success"] = "申請單已送出審核";
            }
            catch (Exception ex)
            {
                // 若流程或權限不符，就顯示錯誤
                TempData["Error"] = ex.Message;
            }

            // 無論成功或失敗都回詳細頁
            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // 7. 主管核准申請單（POST）
        // =========================================================
        [HttpPost]
        [Authorize(Roles = "Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            // 取得目前主管 ID
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // 呼叫 service 執行核准
                await _applicationService.ApproveAsync(id, userId);
                TempData["Success"] = "申請單已審核通過";
            }
            catch (Exception ex)
            {
                // 若流程狀態不允許或權限不符，就顯示錯誤
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // 8. 主管駁回申請單（POST）
        // =========================================================
        [HttpPost]
        [Authorize(Roles = "Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string comment)
        {
            // 取得目前主管 ID
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // 呼叫 service 執行駁回，並傳入駁回意見
                await _applicationService.RejectAsync(id, userId, comment);
                TempData["Success"] = "申請單已駁回";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // 9. 作廢申請單（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoidApplication(int id)
        {
            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // 呼叫 service 將申請單作廢
                await _applicationService.VoidAsync(id, userId);
                TempData["Success"] = "申請單已作廢";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================================================
        // 10. 主管審核歷程頁
        // =========================================================
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> AuditHistory()
        {
            // 取得目前主管 ID
            var managerId = _userManager.GetUserId(User)!;

            // 查這位主管可看到的審核歷史
            var applications = await _applicationService.GetAuditHistoryAsync(managerId);

            return View(applications);
        }

        // =========================================================
        // 11. 判斷目前登入者是否為這張申請單的負責主管
        // =========================================================
        private bool CanAccessAsResponsibleManager(Application app, string userId)
        {
            // 不是主管就直接 false
            if (!User.IsInRole("Manager"))
                return false;

            // 優先抓申請單上的 ApproverId
            // 若沒有，再退回抓員工資料上的 ManagerId
            var responsibleManagerId = app.ApproverId ?? app.Employee?.ManagerId;

            // 必須有值，而且要等於目前登入者 ID
            return !string.IsNullOrEmpty(responsibleManagerId) && responsibleManagerId == userId;
        }

        // =========================================================
        // 12. 把目前資料中的 Application 轉成詳細頁 ViewModel
        // =========================================================
        private static ApplicationDetailViewModel BuildCurrentDetailViewModel(Application app)
        {
            // 先整理所有差旅日期，方便算起訖日
            var dates = app.DailyTrips.Select(d => d.Date).OrderBy(d => d).ToList();

            // 把所有差旅底下的費用攤平，方便算總碳排(把「很多個 List」合併成一個 List)
            var allExpenses = app.DailyTrips.SelectMany(d => d.Expenses).ToList();

            var vm = new ApplicationDetailViewModel
            {
                ApplicationId = app.Id,
                EmployeeName = app.Employee?.Name ?? string.Empty,
                DepartmentName = app.Department?.Name,
                StatusCode = app.Status.Code,
                StatusName = app.Status.Name,
                SubmittedAt = app.SubmittedAt,
                ApprovedAt = app.ApprovedAt,
                ApproverName = app.Approver?.Name,
                VoidedFromStatusCode = app.VoidedFromStatusCode,
                StartDate = dates.FirstOrDefault(),
                EndDate = dates.LastOrDefault(),

                // 加總所有已算出碳排的費用明細
                TotalCo2 = allExpenses
                    .Where(e => e.CarbonEmission != null)
                    .Sum(e => e.CarbonEmission!.TotalCo2),

                // 審核歷程：依時間新到舊排序
                ApprovalHistories = app.ApprovalHistories
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => new ApprovalHistoryItem
                    {
                        CreatedAt = h.CreatedAt,
                        Action = h.Action.ToString(),
                        ActorName = h.Actor?.Name,
                        Comment = h.Comment
                    })
                    .ToList(),

                // 每日差旅摘要：依日期排序，並計算各類費用小計
                DailyTrips = app.DailyTrips
                    .OrderBy(d => d.Date)
                    .Select(d => new DailyTripSummary
                    {
                        DailyTripId = d.Id,
                        Date = d.Date,
                        TripReason = d.TripReason,
                        TransportTotal = d.Expenses.Where(e => e.ExpenseCategory?.Code == "DomesticTransport").Sum(e => e.Amount),
                        MealTotal = d.Expenses.Where(e => e.ExpenseCategory?.Code == "MealAllowance").Sum(e => e.Amount),
                        LodgingTotal = d.Expenses.Where(e => e.ExpenseCategory?.Code == "Lodging").Sum(e => e.Amount),
                        OtherTotal = d.Expenses.Where(e => e.ExpenseCategory?.Code == "Other").Sum(e => e.Amount),
                        Expenses = d.Expenses.ToList()
                    })
                    .ToList()
            };

            // 申請單總金額 = 每日小計加總
            vm.TotalAmount = vm.DailyTrips.Sum(d => d.DayTotal);
            return vm;
        }

        // =========================================================
        // 13. 把「駁回快照資料」轉成詳細頁 ViewModel
        // =========================================================
        private static ApplicationDetailViewModel BuildSnapshotDetailViewModel(Application app, ReportSnapshotData snapshot, DateTime snapshotCreatedAt)
        {
            return new ApplicationDetailViewModel
            {
                ApplicationId = app.Id,
                EmployeeName = snapshot.EmployeeName,
                DepartmentName = app.Department?.Name,
                StatusCode = app.Status.Code,
                StatusName = app.Status.Name,
                SubmittedAt = snapshot.SubmittedAt,
                ApprovedAt = snapshot.ApprovedAt,
                ApproverName = snapshot.ApproverName,
                VoidedFromStatusCode = app.VoidedFromStatusCode,
                IsSnapshotView = true,
                SnapshotCreatedAt = snapshotCreatedAt,
                StartDate = snapshot.StartDate,
                EndDate = snapshot.EndDate,
                TotalAmount = snapshot.TotalAmount,
                TotalCo2 = snapshot.TotalCo2,

                // 歷程仍用目前資料表中的審核紀錄
                ApprovalHistories = app.ApprovalHistories
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => new ApprovalHistoryItem
                    {
                        CreatedAt = h.CreatedAt,
                        Action = h.Action.ToString(),
                        ActorName = h.Actor?.Name,
                        Comment = h.Comment
                    })
                    .ToList(),

                // 每日差旅摘要改用快照中的內容，避免被後續修改影響
                DailyTrips = snapshot.DailyTrips
                    .Select(d => new DailyTripSummary
                    {
                        DailyTripId = d.DailyTripId,
                        Date = d.Date,
                        TripReason = d.TripReason,
                        TransportTotal = d.TransportTotal,
                        MealTotal = d.MealTotal,
                        LodgingTotal = d.LodgingTotal,
                        OtherTotal = d.OtherTotal
                    })
                    .ToList()
            };
        }
    }
}
