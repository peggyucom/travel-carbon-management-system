using Microsoft.AspNetCore.Authorization;   // [Authorize] 權限控管屬性
using Microsoft.AspNetCore.Identity;        // UserManager，用來取得目前登入者
using Microsoft.AspNetCore.Mvc;             // Controller、IActionResult 等 MVC 核心類別
using Microsoft.EntityFrameworkCore;        // EF Core 查詢方法，例如 ToListAsync()
using StarterM.Data;                        // ApplicationDbContext
using StarterM.Models;                      // Entity 模型，例如 DailyTrip、ExpenseRecord
using StarterM.Services.Interfaces;         // Service 介面
using StarterM.ViewModels;                  // ViewModel

namespace StarterM.Controllers
{
    // 整個 Controller 都需要登入後才能使用
    [Authorize]
    public class ExpenseController : Controller
    {
        // 國內交通「合理距離上限」
        // 超過 1000 公里就視為異常資料
        private const decimal MaxDomesticTransportDistanceKm = 1000m;

        // ===== 依賴注入進來的服務 / 物件 =====
        // 費用明細相關 service
        private readonly IExpenseService _expenseService;

        // 每日差旅相關 service
        private readonly IDailyTripService _dailyTripService;

        // 距離計算 service（例如地址算距離）
        private readonly IDistanceService _distanceService;

        // 系統制度參數 service（例如膳雜費、每公里補助）
        private readonly ISystemConfigService _systemConfigService;

        // Identity 的 UserManager，用來取得目前登入使用者
        private readonly UserManager<ApplicationUser> _userManager;

        // 資料庫 context，直接查表用
        private readonly ApplicationDbContext _db;

        // 建構式：由 DI 容器自動把需要的服務注入進來
        public ExpenseController(
            IExpenseService expenseService,
            IDailyTripService dailyTripService,
            IDistanceService distanceService,
            ISystemConfigService systemConfigService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _expenseService = expenseService;
            _dailyTripService = dailyTripService;
            _distanceService = distanceService;
            _systemConfigService = systemConfigService;
            _userManager = userManager;
            _db = db;
        }

        // =========================================================
        // 1. 差旅列表頁：只給 Employee 看自己的「未掛申請單」差旅
        // =========================================================
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Index()
        {
            // 取得目前登入者的使用者 ID
            var userId = _userManager.GetUserId(User)!;

            // 查出這個員工所有每日差旅
            var trips = await _dailyTripService.GetByEmployeeIdAsync(userId);

            // 只保留還沒掛到申請單上的差旅
            var unlinkedTrips = trips.Where(t => t.ApplicationId == null).ToList();

            // 把資料傳給 View 顯示
            return View(unlinkedTrips);
        }

        // =========================================================
        // 2. 顯示「新增每日差旅」頁面（GET）
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public IActionResult CreateTrip(int? applicationId)
        {
            // 回傳空白表單 ViewModel
            // 如果有傳 applicationId，就先帶入，代表這筆差旅準備掛到某張申請單
            return View(new DailyTripCreateViewModel
            {
                ApplicationId = applicationId
            });
        }

        // =========================================================
        // 3. 送出「新增每日差旅」表單（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken] // 防止 CSRF 攻擊
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CreateTrip(DailyTripCreateViewModel model)
        {
            // 先檢查 ViewModel 驗證有沒有通過
            // 例如必填、格式錯誤等等
            if (!ModelState.IsValid)
                return View(model);

            // 商業規則 1：日期不能超過今天
            if (model.Date > DateTime.Today)
            {
                ModelState.AddModelError("Date", "日期不可超過今天");
                return View(model);
            }

            // 商業規則 2：日期不能早於 90 天前
            if (model.Date < DateTime.Today.AddDays(-90))
            {
                ModelState.AddModelError("Date", "日期不可早於 90 天前");
                return View(model);
            }

            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // 呼叫 service 建立每日差旅
                var trip = await _dailyTripService.CreateAsync(
                    userId,
                    model.Date,
                    model.TripReason,
                    model.ApplicationId);

                // 建立成功後，直接跳到這筆差旅的詳細頁
                return RedirectToAction(nameof(TripDetails), new { id = trip.Id });
            }
            catch (InvalidOperationException ex)
            {
                // 如果 service 裡有業務規則不允許，例如日期重複之類
                // 就把錯誤訊息掛回 Date 欄位顯示
                ModelState.AddModelError("Date", ex.Message);
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                // 若 service 認定這個人無權建立
                return Forbid();
            }
        }

        // =========================================================
        // 4. 差旅詳細頁：Employee / Manager 都可看
        //    但主管只能看自己負責的員工資料
        // =========================================================
        [Authorize(Roles = "Employee,Manager")]
        public async Task<IActionResult> TripDetails(int id)
        {
            // 先查這筆差旅是否存在
            var trip = await _dailyTripService.GetByIdAsync(id);
            if (trip == null) return NotFound();

            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            // 如果不是本人，且也不是負責此員工/申請單的主管，就禁止存取
            if (trip.EmployeeId != userId && !CanAccessTripAsResponsibleManager(trip, userId))
                return Forbid();

            // 建立 ViewModel，並決定這筆差旅目前是否可編輯
            //var canEdit = CanEditTrip(trip);

            //var vm = BuildTripDetailViewModel(trip, canEdit);

            //return View(vm);
            //先把 trip 整理成畫面要用的資料（ViewModel），並判斷這筆資料能不能編輯，然後把這些資料丟給 View 顯示。

            return View(BuildTripDetailViewModel(trip, canEditTrip: CanEditTrip(trip)));
        }

        // =========================================================
        // 5. 顯示「新增費用明細」頁面（GET）
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> AddExpense(int tripId)
        {
            // 先查這筆每日差旅
            var trip = await _dailyTripService.GetByIdAsync(tripId);
            if (trip == null) return NotFound();

            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            // 只有這筆差旅的本人能新增費用
            if (trip.EmployeeId != userId) return Forbid();

            // 如果這筆差旅所屬申請單已送出，就不能再新增費用
            var appStatus = trip.Application?.Status?.Code;
            if (appStatus != null && appStatus != "Draft" && appStatus != "Rejected")
            {
                TempData["Error"] = "此差旅所屬申請單已送出，無法新增費用";
                return RedirectToAction(nameof(TripDetails), new { id = tripId });
            }

            // 準備畫面要用的下拉選單 / 參數資料
            await PopulateExpenseViewData(tripId, trip);

            // 回傳空白的費用建立表單
            return View(new ExpenseCreateViewModel { DailyTripId = tripId });
        }

        // =========================================================
        // 6. 送出「新增費用明細」表單（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> AddExpense(ExpenseCreateViewModel model)
        {
            // 先查出這筆每日差旅
            var trip = await _dailyTripService.GetByIdAsync(model.DailyTripId);
            if (trip == null) return NotFound();

            // 取得目前登入者 ID
            var userId = _userManager.GetUserId(User)!;

            // 只有本人可以新增自己的費用明細
            if (trip.EmployeeId != userId) return Forbid();

            // 若申請單狀態不是 Draft / Rejected，就不允許修改
            var appStatus = trip.Application?.Status?.Code;
            if (appStatus != null && appStatus != "Draft" && appStatus != "Rejected")
            {
                TempData["Error"] = "此差旅所屬申請單已送出，無法新增費用";
                return RedirectToAction(nameof(TripDetails), new { id = model.DailyTripId });
            }

            // 基本欄位驗證沒過，回表單頁
            if (!ModelState.IsValid)
            {
                await PopulateExpenseViewData(model.DailyTripId, trip);
                return View(model);
            }

            // 查詢費用分類
            var category = await _db.ExpenseCategories.FindAsync(model.ExpenseCategoryId);

            // 查詢費用項目
            var item = await _db.ExpenseItems.FindAsync(model.ExpenseItemId);

            // 判斷是不是國內交通
            var isDomesticTransport = category?.Code == "DomesticTransport";

            // 判斷是不是自用車
            var isPersonalCar = item?.Code == "PersonalCar";

            // 先建立一筆 ExpenseRecord，先把表單資料塞進去。
            //
            // 注意：
            // 國內交通的 DistanceKm 先故意設成 null，
            // 因為它之後要再依規則決定「用前端算好的距離」或「後端補算距離」，
            // 不希望一開始就直接把 model.DistanceKm 當成最終答案。
            var record = new ExpenseRecord
            {
                EmployeeId = userId,
                DailyTripId = model.DailyTripId,
                Date = trip.Date,
                ExpenseCategoryId = model.ExpenseCategoryId,
                ExpenseItemId = model.ExpenseItemId,
                Amount = model.Amount,
                DistanceKm = isDomesticTransport ? null : model.DistanceKm,
                Description = model.Description,
                Origin = model.Origin,
                Destination = model.Destination,
                IsRoundTrip = model.IsRoundTrip
            };

            // ============================
            // 膳雜費：系統自動帶制度費率
            // ============================
            if (category?.Code == "MealAllowance")
            {
                // 依差旅日期抓當日有效的膳雜費日額
                var rate = await _systemConfigService.GetMealAllowanceDailyRateAsync(trip.Date);

                // 直接用制度費率覆蓋表單金額
                record.Amount = (int)rate;

                // 膳雜費不需要距離
                record.DistanceKm = null;
            }

            // ============================
            // 國內交通：以前端已計算的距離為主，缺值時才由後端補算
            // ============================
            if (isDomesticTransport
                && !string.IsNullOrEmpty(model.Origin)
                && !string.IsNullOrEmpty(model.Destination))
            {
                // roundTripDistance：最後決定要寫進資料庫的距離。
                // 名稱雖然叫 roundTripDistance，
                // 但它的值會依 IsRoundTrip 決定是否已包含往返倍數。
                decimal? roundTripDistance = null;

                // 一般正常流程：
                // 1. 使用者在前端按「計算路線距離」
                // 2. 前端透過 /api/expense/route-preview 拿到距離
                // 3. 距離顯示在畫面上，再連同表單一起送回
                //
                // 因此後端優先吃前端傳回來的 DistanceKm。
                if (model.DistanceKm.HasValue && model.DistanceKm.Value > 0)
                {
                    roundTripDistance = model.DistanceKm.Value;
                }

                // fallback：如果前端沒帶距離，才由後端自己補算。
                // 這是防呆機制，避免前端漏送或少數例外情況時整筆失敗。
                if (!roundTripDistance.HasValue)
                {
                    var route = await _distanceService.CalculateRouteAsync(new DistanceRouteRequestViewModel
                    {
                        Origin = model.Origin,
                        Destination = model.Destination,
                        OriginLatitude = model.OriginLatitude,
                        OriginLongitude = model.OriginLongitude,
                        DestinationLatitude = model.DestinationLatitude,
                        DestinationLongitude = model.DestinationLongitude,
                        IsRoundTrip = model.IsRoundTrip
                    });

                    if (route != null)
                    {
                        roundTripDistance = route.TotalDistanceKm;
                    }
                }

                // 有成功取得距離，才做後續計算與驗證
                if (roundTripDistance.HasValue)
                {
                    // 距離異常檢查：超過 1000 公里就擋下
                    if (roundTripDistance.Value > MaxDomesticTransportDistanceKm)
                    {
                        ModelState.AddModelError(
                            "DistanceKm",
                            "距離異常：單日往返距離超過 1000 公里，請重新定位起訖點並再次計算。");

                        await PopulateExpenseViewData(model.DailyTripId, trip);
                        return View(model);
                    }

                    // 寫回距離
                    record.DistanceKm = roundTripDistance.Value;
                }

                // 自用車：金額不是吃前端 Amount，而是後端根據距離重算。
                // 這樣可以避免有人手改金額，確保補助計算一致。
                if (isPersonalCar && roundTripDistance.HasValue)
                {
                    var carRatePerKm = await _systemConfigService.GetCarAllowancePerKmAsync(trip.Date);
                    record.Amount = (int)Math.Ceiling(roundTripDistance.Value * carRatePerKm);
                }
            }

            // ============================
            // 國內交通：一定要有公里數，因為後面要算碳排
            // ============================
            if (isDomesticTransport)
            {
                // 國內交通一定要有起點與終點，因為距離與碳排都依賴這兩個欄位
                if (string.IsNullOrWhiteSpace(model.Origin))
                {
                    ModelState.AddModelError("Origin", "國內交通費必須填寫起點。");
                }

                if (string.IsNullOrWhiteSpace(model.Destination))
                {
                    ModelState.AddModelError("Destination", "國內交通費必須填寫終點。");
                }

                // 沒有距離或距離 <= 0，視為不合法
                if (!record.DistanceKm.HasValue || record.DistanceKm.Value <= 0)
                {
                    ModelState.AddModelError("DistanceKm", "系統無法計算路線公里數，請確認起訖點後重新計算。");
                }
                // 超過合理上限也不行，避免錯誤定位或異常資料直接入庫
                else if (record.DistanceKm.Value > MaxDomesticTransportDistanceKm)
                {
                    ModelState.AddModelError(
                        "DistanceKm",
                        "距離異常：單日往返距離超過 1000 公里，請重新定位起訖點並再次計算。");
                }

                // 如果這邊補了錯誤，要重新回表單頁
                if (!ModelState.IsValid)
                {
                    await PopulateExpenseViewData(model.DailyTripId, trip);
                    return View(model);
                }
            }

            // 真正寫入資料庫的動作交給 ExpenseService。
            // Controller 在這裡的責任主要是：
            // 1. 頁面流程控制
            // 2. 權限檢查
            // 3. 前端表單資料整理成 record
            // 4. 距離 / 金額等頁面層規則處理
            await _expenseService.CreateAsync(record);

            // 建立完回到差旅詳細頁
            return RedirectToAction(nameof(TripDetails), new { id = model.DailyTripId });
        }

        // =========================================================
        // 7. 刪除單筆費用明細
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> DeleteExpense(int id, int tripId)
        {
            // 查費用明細
            var record = await _expenseService.GetByIdAsync(id);
            if (record == null) return NotFound();

            // 只有費用的本人可以刪
            var userId = _userManager.GetUserId(User)!;
            if (record.EmployeeId != userId) return Forbid();

            // 執行刪除
            await _expenseService.DeleteAsync(id);

            // 顯示成功訊息
            TempData["Success"] = "費用明細已刪除";

            // 回差旅詳細頁
            return RedirectToAction(nameof(TripDetails), new { id = tripId });
        }

        // =========================================================
        // 8. 刪除整筆每日差旅
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            // 查差旅
            var trip = await _dailyTripService.GetByIdAsync(id);
            if (trip == null) return NotFound();

            // 只有本人可刪
            var userId = _userManager.GetUserId(User)!;
            if (trip.EmployeeId != userId) return Forbid();

            // 先記住它原本有沒有掛申請單
            var applicationId = trip.ApplicationId;

            // 執行刪除
            await _dailyTripService.DeleteAsync(id);

            TempData["Success"] = "刪除成功";

            // 如果原本屬於某張申請單，刪完就回申請單明細
            if (applicationId.HasValue)
                return RedirectToAction("Details", "Application", new { id = applicationId.Value });

            // 否則回差旅列表
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 9. 查看單筆費用詳細資料
        // =========================================================
        [Authorize(Roles = "Employee,Manager")]
        public async Task<IActionResult> Details(int id)
        {
            // 查費用明細
            var record = await _expenseService.GetByIdAsync(id);
            if (record == null) return NotFound();

            // 取得目前登入者
            var userId = _userManager.GetUserId(User)!;

            // 若不是本人，也不是負責主管，就禁止
            if (record.EmployeeId != userId && !CanAccessExpenseAsResponsibleManager(record, userId))
                return Forbid();

            // 顯示詳細資料
            return View(record);
        }

        // =========================================================
        // 10. API：依費用分類取得對應的費用項目
        //     給前端 AJAX 動態載入下拉選單用
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetExpenseItems(int categoryId)
        {
            var items = await _db.ExpenseItems
                .Where(i => i.CategoryId == categoryId && i.IsActive) // 同分類 + 啟用中
                .OrderBy(i => i.SortOrder)                            // 依排序欄位顯示
                .Select(i => new { i.Id, i.Name, i.Code })            // 只傳前端需要的欄位
                .ToListAsync();

            return Json(items);
        }

        // =========================================================
        // 11. 準備新增費用頁面要用的 ViewData
        // =========================================================
        private async Task PopulateExpenseViewData(int tripId, DailyTrip trip)
        {
            // 差旅基本資料
            ViewData["TripId"] = tripId;
            ViewData["TripDate"] = trip.Date.ToString("yyyy-MM-dd");
            ViewData["TripReason"] = trip.TripReason;

            // 制度參數：膳雜費日額
            ViewData["MealAllowanceDailyRate"] =
                await _systemConfigService.GetMealAllowanceDailyRateAsync(trip.Date);

            // 制度參數：自用車每公里補助
            ViewData["CarAllowancePerKm"] =
                await _systemConfigService.GetCarAllowancePerKmAsync(trip.Date);

            // 費用分類下拉選單：只抓啟用中分類
            var categories = await _db.ExpenseCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            ViewData["ExpenseCategories"] = categories;
        }

        // =========================================================
        // 12. 判斷目前登入者是否為「可查看這筆差旅的主管」
        // =========================================================
        private bool CanAccessTripAsResponsibleManager(DailyTrip trip, string userId)
        {
            // 不是主管就直接 false
            if (!User.IsInRole("Manager"))
                return false;

            // 優先抓申請單上的 ApproverId
            // 若沒有，再退回抓員工資料上的 ManagerId
            var responsibleManagerId = trip.Application?.ApproverId ?? trip.Employee?.ManagerId;

            // 必須有值，而且要等於目前登入者的 ID
            return !string.IsNullOrEmpty(responsibleManagerId) && responsibleManagerId == userId;
        }

        // =========================================================
        // 13. 判斷目前登入者是否為「可查看這筆費用的主管」
        // =========================================================
        private bool CanAccessExpenseAsResponsibleManager(ExpenseRecord record, string userId)
        {
            // 不是主管，不能看
            if (!User.IsInRole("Manager"))
                return false;

            // 先找這筆費用所屬差旅的申請單簽核主管
            // 沒有的話，再退回抓員工直屬主管
            var responsibleManagerId =
                record.DailyTrip?.Application?.ApproverId ?? record.Employee?.ManagerId;

            return !string.IsNullOrEmpty(responsibleManagerId) && responsibleManagerId == userId;
        }

        // =========================================================
        // 14. 判斷一筆差旅目前能不能編輯
        // =========================================================
        private bool CanEditTrip(DailyTrip trip)
        {
            // 主管不能編輯員工差旅
            if (User.IsInRole("Manager"))
                return false;

            // 若沒有申請單，或狀態為 Draft / Rejected，才允許編輯
            var appStatus = trip.Application?.Status?.Code;
            return appStatus == null || appStatus == "Draft" || appStatus == "Rejected";
        }

        // =========================================================
        // 15. 把 DailyTrip 轉成給 View 用的 TripDetailViewModel
        // =========================================================
        private static TripDetailViewModel BuildTripDetailViewModel(DailyTrip trip, bool canEditTrip)
        {
            return new TripDetailViewModel
            {
                DailyTripId = trip.Id,
                ApplicationId = trip.ApplicationId,
                Date = trip.Date,
                TripReason = trip.TripReason,
                CanEditTrip = canEditTrip,

                // 把明細依建立時間排序後，轉成前端要顯示的 ViewModel
                Expenses = trip.Expenses
                    .OrderBy(e => e.CreatedAt)
                    .Select(e => new TripExpenseItemViewModel
                    {
                        ExpenseId = e.Id,
                        CategoryName = e.ExpenseCategory?.Name ?? "-",
                        ItemName = e.ExpenseItem?.Name ?? "-",
                        Amount = e.Amount,
                        DistanceKm = e.DistanceKm,
                        EstimatedCo2 = e.CarbonEmission?.TotalCo2,
                        Origin = e.Origin,
                        Destination = e.Destination,
                        IsRoundTrip = e.IsRoundTrip,
                        Description = e.Description
                    })
                    .ToList()
            };
        }
    }
}
