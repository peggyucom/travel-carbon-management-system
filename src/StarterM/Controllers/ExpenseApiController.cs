using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterM.Services.Interfaces;
using StarterM.ViewModels;

namespace StarterM.Controllers
{
    // ExpenseApiController：
    // 專門提供前端 JavaScript 呼叫的 JSON API。
    //
    // 這支 controller 和 MVC 的 ExpenseController 分工不同：
    // - ExpenseController：負責回 Razor 頁面、收表單、處理導頁
    // - ExpenseApiController：負責前端 fetch 時需要的資料 API
    //
    // 這樣做的好處是把「頁面流程」和「AJAX / JSON」拆開，
    // 後續要維護路線預覽、地址查詢等功能時，責任會更清楚。
    [Authorize]
    [Route("api/expense")]
    [ApiController]
    public class ExpenseApiController : ControllerBase
    {
        // 單筆費用查詢 / 刪除
        private readonly IExpenseService _expenseService;

        // 地址 geocode 與路線距離計算
        private readonly IDistanceService _distanceService;

        // 目前這支檔案尚未直接使用到 userManager，
        // 但保留注入可方便未來擴充「只能看自己的資料」等 API 規則。
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;

        // 建構式：由 DI 容器注入所需服務
        public ExpenseApiController(
            IExpenseService expenseService,
            IDistanceService distanceService,
            Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
        {
            _expenseService = expenseService;
            _distanceService = distanceService;
            _userManager = userManager;
        }

        // 取得單筆費用資料
        // 用途：前端若需要動態載入明細，可直接呼叫此 API。
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var record = await _expenseService.GetByIdAsync(id);
            if (record == null)
                return NotFound(ApiResponse.Fail("NOT_FOUND", "找不到費用紀錄"));

            return Ok(ApiResponse<object>.Ok(record));
        }

        // 刪除單筆費用資料
        // 用途：若前端未來改成 AJAX 刪除，可直接走這支 API。
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _expenseService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse.Fail("NOT_FOUND", "找不到費用紀錄"));

            return Ok(ApiResponse.Ok());
        }

        // Geocode API：
        // 前端送入地址字串，後端再統一呼叫 OpenStreetMap 的 Nominatim。
        //
        // 這樣前端不需要知道外部服務網址，
        // 也方便未來統一更換供應商或補上限制規則。
        [HttpGet("geocode")]
        public async Task<IActionResult> Geocode([FromQuery] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return BadRequest(ApiResponse.Fail("INVALID_ADDRESS", "請輸入地址"));

            var result = await _distanceService.GeocodeAsync(address);
            if (result == null)
                return NotFound(ApiResponse.Fail("GEOCODE_NOT_FOUND", "找不到此地址"));

            return Ok(ApiResponse<DistanceGeocodeResultViewModel>.Ok(result));
        }

        // 路線預覽 API：
        // 前端送起點 / 終點 / 是否往返 / 可用座標，
        // 後端交給 DistanceService 統一算出：
        // 1. 單程距離
        // 2. 總距離
        // 3. 前端可直接繪圖的 geometry
        [HttpPost("route-preview")]
        public async Task<IActionResult> RoutePreview([FromBody] DistanceRouteRequestViewModel request)
        {
            if (request == null)
                return BadRequest(ApiResponse.Fail("INVALID_REQUEST", "缺少路線資料"));

            var result = await _distanceService.CalculateRouteAsync(request);
            if (result == null)
                return BadRequest(ApiResponse.Fail("ROUTE_NOT_FOUND", "無法計算此起訖點的路線"));

            return Ok(ApiResponse<DistanceRouteResultViewModel>.Ok(result));
        }
    }
}
