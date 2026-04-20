using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterM.Services.Interfaces;
using StarterM.ViewModels.Dashboard;
using StarterM.ViewModels;
using System.Security.Claims;

namespace StarterM.Controllers
{
    [Authorize(Roles = "Manager")]
    public class DashboardController : Controller
    {
        private const int DefaultRankingLimit = 10;

        private readonly IReportService _reportService;

        public DashboardController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeDetail(
            [FromQuery] string employeeId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string view = "cost")
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return RedirectToAction(nameof(Index));
            }

            var today = DateTime.Today;
            startDate ??= new DateTime(today.Year, today.Month, 1);
            endDate ??= today;

            if (!TryResolveDateRange(startDate, endDate, out var resolvedStart, out var resolvedEnd, out _))
            {
                return RedirectToAction(nameof(Index));
            }

            var resolvedView = string.Equals(view, "carbon", StringComparison.OrdinalIgnoreCase)
                ? "carbon"
                : "cost";

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            var employeeName = await _reportService.GetScopedEmployeeNameAsync(currentUserId, employeeId);
            if (string.IsNullOrWhiteSpace(employeeName))
            {
                return NotFound();
            }

            var model = new EmployeeTravelDetailPageViewModel
            {
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                View = resolvedView,
                StartDate = resolvedStart,
                EndDate = resolvedEnd
            };

            return View(model);
        }

        [HttpGet]
        [Route("api/dashboard/kpi")]
        public async Task<IActionResult> GetKpi([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
                (currentUserId, start, end) => _reportService.GetDashboardKpiAsync(currentUserId, start, end));
        }

        [HttpGet]
        [Route("api/dashboard/travel-cost-trend")]
        public async Task<IActionResult> GetTravelCostTrend([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
                (currentUserId, start, end) => _reportService.GetTravelCostTrendAsync(currentUserId, start, end));
        }

        [HttpGet]
        [Route("api/dashboard/expense-category-distribution")]
        public async Task<IActionResult> GetExpenseCategoryDistribution([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
                (currentUserId, start, end) => _reportService.GetExpenseCategoryDistributionAsync(currentUserId, start, end));
        }

        [HttpGet]
        [Route("api/dashboard/cost-carbon-trend")]
        public async Task<IActionResult> GetCostCarbonTrend([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
                (currentUserId, start, end) => _reportService.GetCostCarbonTrendAsync(currentUserId, start, end));
        }

        [HttpGet]
        [Route("api/dashboard/employee-expense-ranking")]
        public async Task<IActionResult> GetEmployeeExpenseRanking(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int limit = DefaultRankingLimit)
        {
            limit = NormalizeLimit(limit);

            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
                (currentUserId, start, end) => _reportService.GetEmployeeExpenseRankingAsync(currentUserId, start, end, limit));
        }

        [HttpGet]
        [Route("api/dashboard/employee-carbon-ranking")]
        public async Task<IActionResult> GetEmployeeCarbonRanking(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int limit = DefaultRankingLimit)
        {
            limit = NormalizeLimit(limit);

            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
                (currentUserId, start, end) => _reportService.GetEmployeeCarbonRankingAsync(currentUserId, start, end, limit));
        }

        [HttpGet]
        [Route("api/dashboard/transport-carbon-share")]
        public async Task<IActionResult> GetTransportCarbonShare([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
            (currentUserId, start, end) => _reportService.GetTransportCarbonShareAsync(currentUserId, start, end));
        }

        [HttpGet]
        [Route("api/dashboard/transport-cost-carbon")]
        public async Task<IActionResult> GetTransportCostCarbon([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
            (currentUserId, start, end) => _reportService.GetTransportCostCarbonAsync(currentUserId, start, end));
        }

        [HttpGet]
        [Route("api/dashboard/employee-detail")]
        public async Task<IActionResult> GetEmployeeDetail(
            [FromQuery] string employeeId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return BadRequest(ApiResponse.Fail("INVALID_EMPLOYEE", "請提供員工識別碼。"));
            }

            return await ExecuteDashboardQueryAsync(
                startDate,
                endDate,
                (currentUserId, start, end) => _reportService.GetEmployeeTravelDetailAsync(currentUserId, employeeId, start, end));
        }

        private async Task<IActionResult> ExecuteDashboardQueryAsync<T>(
            DateTime? startDate,
            DateTime? endDate,
            Func<string, DateTime, DateTime, Task<T>> queryAction)
        {
            if (!TryResolveDateRange(startDate, endDate, out var resolvedStart, out var resolvedEnd, out var errorResult))
            {
                return errorResult!;
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(ApiResponse.Fail("UNAUTHORIZED", "找不到目前登入的主管資訊。"));
            }

            var data = await queryAction(currentUserId, resolvedStart, resolvedEnd);
            return Ok(ApiResponse<T>.Ok(data));
        }

        private static bool TryResolveDateRange(
            DateTime? startDate,
            DateTime? endDate,
            out DateTime resolvedStart,
            out DateTime resolvedEnd,
            out IActionResult? errorResult)
        {
            resolvedStart = default;
            resolvedEnd = default;
            errorResult = null;

            if (!startDate.HasValue || !endDate.HasValue)
            {
                errorResult = new BadRequestObjectResult(
                    ApiResponse.Fail("INVALID_DATE_RANGE", "請提供完整的開始日期與結束日期。"));
                return false;
            }

            resolvedStart = startDate.Value.Date;
            resolvedEnd = endDate.Value.Date;

            if (resolvedStart > resolvedEnd)
            {
                errorResult = new BadRequestObjectResult(
                    ApiResponse.Fail("INVALID_DATE_RANGE", "開始日期不可晚於結束日期。"));
                return false;
            }

            return true;
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0)
            {
                return DefaultRankingLimit;
            }

            return Math.Min(limit, 20);
        }
    }
}
