using Microsoft.AspNetCore.Authorization;   // [Authorize] 權限控管屬性
using Microsoft.AspNetCore.Identity;        // UserManager，用來管理帳號與角色
using Microsoft.AspNetCore.Mvc;             // Controller、IActionResult 等 MVC 核心類別
using Microsoft.EntityFrameworkCore;        // EF Core 查詢方法，例如 Include、ToListAsync()
using StarterM.Data;                        // ApplicationDbContext
using StarterM.Models;                      // Entity 模型，例如 ApplicationUser
using StarterM.Services.Interfaces;         // Service 介面
using StarterM.ViewModels;                  // ViewModel

namespace StarterM.Controllers
{
    // 整個 Controller 只允許 Manager 使用
    [Authorize(Roles = "Manager")]
    public class AdminController : Controller
    {
        // ===== 依賴注入進來的服務 / 物件 =====
        // Identity 的 UserManager，用來管理員工帳號
        private readonly UserManager<ApplicationUser> _userManager;

        // 費用明細 service，用來查員工的費用紀錄
        private readonly IExpenseService _expenseService;

        // 系統參數 service，用來維護費率與碳排係數
        private readonly ISystemConfigService _systemConfigService;

        // 資料庫 context，直接查歷史資料與設定資料用
        private readonly ApplicationDbContext _db;

        // 建構式：由 DI 容器自動把需要的服務注入進來
        public AdminController(
            UserManager<ApplicationUser> userManager,
            IExpenseService expenseService,
            ISystemConfigService systemConfigService,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _expenseService = expenseService;
            _systemConfigService = systemConfigService;
            _db = db;
        }

        // =========================================================
        // 1. 管理首頁：顯示目前主管底下的員工清單
        // =========================================================
        public IActionResult Index()
        {
            // 取得目前登入主管的 ID
            var managerId = _userManager.GetUserId(User)!;

            // 查出這位主管底下所有員工，依姓名排序
            var employees = _userManager.Users
                .Where(u => u.ManagerId == managerId)
                .OrderBy(u => u.Name)
                .ToList();

            return View(employees);
        }

        // =========================================================
        // 2. 顯示「新增員工」頁面（GET）
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            // 回傳空白表單 ViewModel
            return View(new EmployeeCreateViewModel());
        }

        // =========================================================
        // 3. 新增員工帳號（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeCreateViewModel model)
        {
            // 基本欄位驗證沒過，就回表單頁
            if (!ModelState.IsValid)
                return View(model);

            // 檢查 Email 是否已經被使用
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "此電子郵件已被使用");
                return View(model);
            }

            // 取得目前登入主管的完整帳號資料
            var manager = await _userManager.GetUserAsync(User);
            if (manager == null)
            {
                return Unauthorized();
            }

            // 主管自己必須先綁定部門，才有辦法建立員工帳號
            if (!manager.DepartmentId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "目前主管帳號尚未設定部門，無法建立員工帳號。");
                return View(model);
            }

            // 建立新的員工帳號物件
            var employee = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                Role = "Employee",
                IsActive = true,
                ManagerId = manager.Id,
                DepartmentId = manager.DepartmentId.Value,
                EmailConfirmed = true
            };

            // 建立帳號與密碼
            var result = await _userManager.CreateAsync(employee, model.Password);
            if (!result.Succeeded)
            {
                // 如果 Identity 驗證失敗，就把錯誤掛回畫面
                foreach (var error in result.Errors)
                    ModelState.AddModelError(nameof(model.Password), error.Description);
                return View(model);
            }

            // 建立成功後，補上 Employee 角色
            await _userManager.AddToRoleAsync(employee, "Employee");

            TempData["Success"] = $"員工「{model.Name}」已新增";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 4. 啟用員工帳號
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            // 取得目前主管 ID
            var managerId = _userManager.GetUserId(User)!;

            // 只能操作自己底下的員工
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null || employee.ManagerId != managerId) return NotFound();

            // 啟用帳號，並解除鎖定
            employee.IsActive = true;
            employee.LockoutEnabled = false;
            employee.LockoutEnd = null;
            employee.UpdatedAt = DateTime.UtcNow;

            // 儲存更新
            var result = await _userManager.UpdateAsync(employee);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                TempData["Error"] = $"員工「{employee.Name}」啟用失敗";
                return RedirectToAction(nameof(Index));
            }

            // 更新 SecurityStamp，讓既有登入狀態能重新驗證
            await _userManager.UpdateSecurityStampAsync(employee);
            TempData["Success"] = $"員工「{employee.Name}」已啟用";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 5. 停用員工帳號
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            // 取得目前主管 ID
            var managerId = _userManager.GetUserId(User)!;

            // 只能操作自己底下的員工
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null || employee.ManagerId != managerId) return NotFound();

            // 停用帳號，並把帳號永久鎖住
            employee.IsActive = false;
            employee.LockoutEnabled = true;
            employee.LockoutEnd = DateTimeOffset.MaxValue;
            employee.UpdatedAt = DateTime.UtcNow;

            // 儲存更新
            var result = await _userManager.UpdateAsync(employee);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                TempData["Error"] = $"員工「{employee.Name}」停用失敗";
                return RedirectToAction(nameof(Index));
            }

            // 更新 SecurityStamp，避免舊登入狀態繼續有效
            await _userManager.UpdateSecurityStampAsync(employee);
            TempData["Success"] = $"員工「{employee.Name}」已停用";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 6. 查看某位員工的費用紀錄
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EmployeeRecords(string id)
        {
            // 取得目前主管 ID
            var managerId = _userManager.GetUserId(User)!;

            // 只能查看自己底下員工的資料
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null || employee.ManagerId != managerId) return NotFound();

            // 查該員工所有費用明細
            var records = await _expenseService.GetByEmployeeIdAsync(id);

            // 只顯示所屬申請單已送出（非草稿）的費用紀錄
            records = records.Where(r =>
                r.DailyTrip?.Application != null &&
                r.DailyTrip.Application.Status?.Code != "Draft")
                .ToList();

            // 把員工資訊放進 ViewData，方便畫面顯示
            ViewData["EmployeeName"] = employee.Name;
            ViewData["EmployeeEmail"] = employee.Email;
            return View(records);
        }

        // ═══ 膳雜費費率維護 ═══

        // =========================================================
        // 7. 顯示膳雜費費率維護頁（GET）
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MealAllowance()
        {
            // 目前生效中的日費率
            ViewData["CurrentEffectiveRate"] = await _systemConfigService.GetMealAllowanceDailyRateAsync(DateTime.Today);

            // 歷史紀錄：依生效日、更新時間排序
            var history = await _db.MealAllowanceHistories
                .Include(h => h.UpdatedBy)
                .OrderByDescending(h => h.EffectiveFrom)
                .ThenByDescending(h => h.UpdatedAt)
                .ThenByDescending(h => h.Id)
                .ToListAsync();

            ViewData["History"] = history;
            return View();
        }

        // =========================================================
        // 8. 更新膳雜費費率（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MealAllowance(int rate, DateTime effectiveFrom)
        {
            var today = DateTime.Today;
            var maxDate = today.AddDays(7);

            // 驗證費率：100~1000 且必須是 50 的倍數
            if (rate < 100 || rate > 1000 || rate % 50 != 0)
                ModelState.AddModelError("rate", "費率需介於 100 ~ 1,000 元，且為 50 的倍數");

            // 驗證生效日：只能設定今天到 7 天內
            if (effectiveFrom.Date < today || effectiveFrom.Date > maxDate)
                ModelState.AddModelError("effectiveFrom", $"生效日期需介於今天（{today:yyyy/MM/dd}）至 {maxDate:yyyy/MM/dd}");

            // 驗證失敗就重新顯示頁面與歷史資料
            if (!ModelState.IsValid)
            {
                ViewData["CurrentEffectiveRate"] = await _systemConfigService.GetMealAllowanceDailyRateAsync(DateTime.Today);
                ViewData["History"] = await _db.MealAllowanceHistories
                    .Include(h => h.UpdatedBy)
                    .OrderByDescending(h => h.EffectiveFrom)
                    .ThenByDescending(h => h.UpdatedAt)
                    .ThenByDescending(h => h.Id)
                    .ToListAsync();
                return View();
            }

            // 呼叫 service 更新費率
            var managerId = _userManager.GetUserId(User)!;
            await _systemConfigService.SetMealAllowanceDailyRateAsync(rate, managerId, effectiveFrom.Date);

            TempData["Success"] = $"膳雜費日費率已更新為 NT$ {rate:N0}，生效日期：{effectiveFrom:yyyy/MM/dd}";
            return RedirectToAction(nameof(MealAllowance));
        }

        // ═══ 自用車補助費率維護 ═══

        // =========================================================
        // 9. 顯示自用車補助費率維護頁（GET）
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> CarAllowance()
        {
            // 目前生效中的每公里補助費率
            ViewData["CurrentEffectiveRate"] = await _systemConfigService.GetCarAllowancePerKmAsync(DateTime.Today);

            // 歷史紀錄：依生效日、更新時間排序
            var history = await _db.CarAllowanceHistories
                .Include(h => h.UpdatedBy)
                .OrderByDescending(h => h.EffectiveFrom)
                .ThenByDescending(h => h.UpdatedAt)
                .ThenByDescending(h => h.Id)
                .ToListAsync();

            ViewData["History"] = history;
            return View();
        }

        // =========================================================
        // 10. 更新自用車補助費率（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CarAllowance(decimal rate, DateTime effectiveFrom)
        {
            var today = DateTime.Today;
            var maxDate = today.AddDays(7);

            // 驗證費率：1~10 元，可到小數 1 位
            if (rate < 1m || rate > 10m)
                ModelState.AddModelError(string.Empty, "費率需介於 1 ~ 10 元（可設定到小數 1 位）");

            // 驗證生效日：只能設定今天到 7 天內
            if (effectiveFrom.Date < today || effectiveFrom.Date > maxDate)
                ModelState.AddModelError("effectiveFrom", $"生效日期需介於今天（{today:yyyy/MM/dd}）至 {maxDate:yyyy/MM/dd}");

            // 驗證失敗就重新顯示頁面與歷史資料
            if (!ModelState.IsValid)
            {
                ViewData["CurrentEffectiveRate"] = await _systemConfigService.GetCarAllowancePerKmAsync(DateTime.Today);
                ViewData["History"] = await _db.CarAllowanceHistories
                    .Include(h => h.UpdatedBy)
                    .OrderByDescending(h => h.EffectiveFrom)
                    .ThenByDescending(h => h.UpdatedAt)
                    .ThenByDescending(h => h.Id)
                    .ToListAsync();
                return View();
            }

            // 呼叫 service 更新費率
            var managerId = _userManager.GetUserId(User)!;
            await _systemConfigService.SetCarAllowancePerKmAsync(rate, managerId, effectiveFrom.Date);

            TempData["Success"] = $"自用車補助費率已更新為 NT$ {rate:N1} / 公里，生效日期：{effectiveFrom:yyyy/MM/dd}";
            return RedirectToAction(nameof(CarAllowance));
        }

        // ═══ 碳排係數管理 ═══

        // =========================================================
        // 11. 碳排係數列表頁
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EmissionFactors()
        {
            var today = DateTime.Today;

            // 先查所有啟用中的交通工具類型
            var vehicleTypes = await _db.VehicleTypes
                .Where(v => v.IsActive)
                .OrderBy(v => v.SortOrder)
                .ThenBy(v => v.Name)
                .ToListAsync();

            // 查今日有效的碳排係數資料
            var factors = await _db.EmissionFactors
                .Include(f => f.UpdatedBy)
                .Include(f => f.VehicleType)
                .Where(f => f.EffectiveFrom <= today && f.VehicleType.IsActive)
                .OrderBy(f => f.VehicleType.SortOrder)
                .ThenByDescending(f => f.EffectiveFrom)
                .ThenByDescending(f => f.UpdatedAt)
                .ThenByDescending(f => f.Id)
                .ToListAsync();

            // 每個交通工具只取目前最新的一筆有效係數
            var currentFactors = factors
                .GroupBy(f => f.VehicleTypeId)
                .ToDictionary(g => g.Key, g => g.First());

            // 組成列表頁要用的 ViewModel
            var model = vehicleTypes.Select(vehicleType =>
            {
                currentFactors.TryGetValue(vehicleType.Id, out var currentFactor);

                return new EmissionFactorListItemViewModel
                {
                    VehicleTypeId = vehicleType.Id,
                    VehicleTypeName = vehicleType.Name,
                    EmissionFactorId = currentFactor?.Id,
                    Co2PerKm = currentFactor?.Co2PerKm,
                    Source = currentFactor?.Source,
                    EffectiveFrom = currentFactor?.EffectiveFrom,
                    UpdatedAt = currentFactor?.UpdatedAt,
                    UpdatedByName = currentFactor?.UpdatedBy?.Name
                };
            }).ToList();

            return View(model);
        }

        // =========================================================
        // 12. 顯示新增碳排係數頁面（GET）
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> CreateEmissionFactor(int vehicleTypeId)
        {
            // 建立畫面初始 ViewModel
            var model = await BuildEmissionFactorViewModelAsync(vehicleTypeId, DateTime.Today);
            if (model == null) return NotFound();

            return View(model);
        }

        // =========================================================
        // 13. 新增碳排係數（POST）
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmissionFactor(EmissionFactorViewModel model)
        {
            var today = DateTime.Today;
            var maxDate = today.AddDays(7);

            // 生效日限制：今天到 7 天內
            if (model.EffectiveFrom.Date < today || model.EffectiveFrom.Date > maxDate)
                ModelState.AddModelError(nameof(model.EffectiveFrom), $"生效日期需介於今天（{today:yyyy/MM/dd}）至 {maxDate:yyyy/MM/dd}");

            // 驗證失敗時，重新補齊畫面必要資料
            if (!ModelState.IsValid)
            {
                if (!await PopulateEmissionFactorViewModelAsync(model))
                    return NotFound();

                return View(model);
            }

            // 呼叫 service 寫入新的碳排係數
            var managerId = _userManager.GetUserId(User)!;
            await _systemConfigService.CreateEmissionFactorAsync(
                model.VehicleTypeId,
                model.Co2PerKm,
                model.Source,
                model.EffectiveFrom.Date,
                managerId);

            TempData["Success"] = $"{model.VehicleTypeName} 碳排係數已新增，生效日期：{model.EffectiveFrom:yyyy/MM/dd}";
            return RedirectToAction(nameof(EmissionFactors));
        }

        // =========================================================
        // 14. 查看某種交通工具的碳排係數歷史
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EmissionFactorHistory(int vehicleTypeId)
        {
            // 先確認交通工具類型存在而且啟用中
            var vehicleType = await _db.VehicleTypes
                .FirstOrDefaultAsync(v => v.Id == vehicleTypeId && v.IsActive);
            if (vehicleType == null) return NotFound();

            // 查這個交通工具的所有碳排係數歷史
            var history = await _db.EmissionFactors
                .Include(f => f.UpdatedBy)
                .Include(f => f.VehicleType)
                .Where(f => f.VehicleTypeId == vehicleTypeId)
                .OrderByDescending(f => f.EffectiveFrom)
                .ThenByDescending(f => f.UpdatedAt)
                .ThenByDescending(f => f.Id)
                .ToListAsync();

            ViewData["VehicleType"] = vehicleType.Name;
            return View(history);
        }

        // =========================================================
        // 15. 費率管理首頁
        //     提供導向膳雜費率 / 自用車補助費率
        // =========================================================
        [HttpGet]
        public IActionResult Rates()
        {
            return View();
        }

        // =========================================================
        // 16. 建立新增碳排係數頁面所需的初始 ViewModel
        // =========================================================
        private async Task<EmissionFactorViewModel?> BuildEmissionFactorViewModelAsync(int vehicleTypeId, DateTime effectiveFrom)
        {
            // 查交通工具類型，只允許啟用中的資料
            var vehicleType = await _db.VehicleTypes
                .Where(v => v.Id == vehicleTypeId && v.IsActive)
                .Select(v => new { v.Id, v.Name })
                .FirstOrDefaultAsync();

            if (vehicleType == null)
                return null;

            return new EmissionFactorViewModel
            {
                VehicleTypeId = vehicleType.Id,
                VehicleTypeName = vehicleType.Name,
                EffectiveFrom = effectiveFrom,
                MinEffectiveFrom = DateTime.Today,
                MaxEffectiveFrom = DateTime.Today.AddDays(7)
            };
        }

        // =========================================================
        // 17. 驗證失敗後，重新補齊碳排係數表單所需資料
        // =========================================================
        private async Task<bool> PopulateEmissionFactorViewModelAsync(EmissionFactorViewModel model)
        {
            // 重新抓交通工具名稱，避免回畫面時資料不完整
            var vehicleType = await _db.VehicleTypes
                .Where(v => v.Id == model.VehicleTypeId && v.IsActive)
                .Select(v => v.Name)
                .FirstOrDefaultAsync();

            if (vehicleType == null)
                return false;

            model.VehicleTypeName = vehicleType;
            model.MinEffectiveFrom = DateTime.Today;
            model.MaxEffectiveFrom = DateTime.Today.AddDays(7);
            return true;
        }
    }
}
