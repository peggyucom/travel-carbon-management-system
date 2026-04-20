using Microsoft.EntityFrameworkCore;
using StarterM.Data;
using StarterM.Models;
using StarterM.Services.Interfaces;

namespace StarterM.Services.Implementations
{
    public class ReportService : IReportService
    {
        private const string TransportCategoryCode = "DomesticTransport";

        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardKpiData> GetDashboardKpiAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var query = await BuildScopedExpenseQueryAsync(currentUserId, startDate, endDate);

            var summary = await query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalTravelCost = g.Sum(x => (decimal)x.Amount),
                    TransportationCost = g.Sum(x => x.ExpenseCategory.Code == TransportCategoryCode ? (decimal)x.Amount : 0m),
                    TotalCarbonEmission = g.Sum(x => x.CarbonEmission != null ? x.CarbonEmission.TotalCo2 : 0m),
                    TravelDays = g.Where(x => x.DailyTripId.HasValue)
                        .Select(x => x.DailyTripId!.Value)
                        .Distinct()
                        .Count()
                })
                .FirstOrDefaultAsync();

            return new DashboardKpiData
            {
                TotalTravelCost = summary?.TotalTravelCost ?? 0m,
                TransportationCost = summary?.TransportationCost ?? 0m,
                TotalCarbonEmission = summary?.TotalCarbonEmission ?? 0m,
                TravelDays = summary?.TravelDays ?? 0
            };
        }

        public async Task<List<TravelCostTrendData>> GetTravelCostTrendAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var query = await BuildScopedExpenseQueryAsync(currentUserId, startDate, endDate);

            var rows = await query
                .GroupBy(e => new { e.Date.Year, e.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    TotalTravelCost = g.Sum(x => (decimal)x.Amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return rows
                .Select(x => new TravelCostTrendData
                {
                    Month = $"{x.Year}-{x.Month:D2}",
                    TotalTravelCost = x.TotalTravelCost
                })
                .ToList();
        }

        public async Task<List<ExpenseCategoryDistributionData>> GetExpenseCategoryDistributionAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var query = await BuildScopedExpenseQueryAsync(currentUserId, startDate, endDate);

            var rows = await query
                .GroupBy(e => new { e.ExpenseCategory.Code, e.ExpenseCategory.Name, e.ExpenseCategory.SortOrder })
                .Select(g => new ExpenseCategoryDistributionData
                {
                    CategoryCode = g.Key.Code,
                    CategoryName = g.Key.Name,
                    TotalExpense = g.Sum(x => (decimal)x.Amount),
                    SortOrder = g.Key.SortOrder
                })
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            var totalExpense = rows.Sum(x => x.TotalExpense);
            foreach (var row in rows)
            {
                row.Percentage = totalExpense > 0
                    ? Math.Round(row.TotalExpense / totalExpense * 100, 1)
                    : 0;
            }

            return rows;
        }

        public async Task<List<CostCarbonTrendData>> GetCostCarbonTrendAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var query = await BuildScopedExpenseQueryAsync(currentUserId, startDate, endDate);

            var rows = await query
                .GroupBy(e => new { e.Date.Year, e.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    TransportationCost = g.Sum(x => x.ExpenseCategory.Code == TransportCategoryCode ? (decimal)x.Amount : 0m),
                    CarbonEmission = g.Sum(x => x.CarbonEmission != null ? x.CarbonEmission.TotalCo2 : 0m)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return rows
                .Select(x => new CostCarbonTrendData
                {
                    Month = $"{x.Year}-{x.Month:D2}",
                    TransportationCost = x.TransportationCost,
                    CarbonEmission = x.CarbonEmission
                })
                .ToList();
        }

        public async Task<List<EmployeeExpenseRankingData>> GetEmployeeExpenseRankingAsync(string currentUserId, DateTime startDate, DateTime endDate, int limit)
        {
            var query = await BuildScopedExpenseQueryAsync(currentUserId, startDate, endDate);

            return await query
                .GroupBy(e => new { e.EmployeeId, e.Employee.Name })
                .Select(g => new EmployeeExpenseRankingData
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.Name,
                    TotalExpense = g.Sum(x => (decimal)x.Amount)
                })
                .OrderByDescending(x => x.TotalExpense)
                .ThenBy(x => x.EmployeeName)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<EmployeeCarbonRankingData>> GetEmployeeCarbonRankingAsync(string currentUserId, DateTime startDate, DateTime endDate, int limit)
        {
            var query = await BuildScopedExpenseQueryAsync(currentUserId, startDate, endDate);

            return await query
                .GroupBy(e => new { e.EmployeeId, e.Employee.Name })
                .Select(g => new EmployeeCarbonRankingData
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.Name,
                    TotalCarbon = g.Sum(x => x.CarbonEmission != null ? x.CarbonEmission.TotalCo2 : 0m)
                })
                .Where(x => x.TotalCarbon > 0)
                .OrderByDescending(x => x.TotalCarbon)
                .ThenBy(x => x.EmployeeName)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<TransportCarbonShareData>> GetTransportCarbonShareAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var query = await BuildScopedCarbonExpenseQueryAsync(currentUserId, startDate, endDate);

            var rows = await query
                .GroupBy(e => e.CarbonEmission!.VehicleType)
                .Select(g => new TransportCarbonShareData
                {
                    TransportType = g.Key,
                    TotalCarbon = g.Sum(x => x.CarbonEmission!.TotalCo2)
                })
                .OrderByDescending(x => x.TotalCarbon)
                .ToListAsync();

            var totalCarbon = rows.Sum(x => x.TotalCarbon);
            foreach (var row in rows)
            {
                row.Percentage = totalCarbon > 0
                    ? Math.Round(row.TotalCarbon / totalCarbon * 100, 1)
                    : 0;
            }

            return rows;
        }

        public async Task<List<TransportCostCarbonData>> GetTransportCostCarbonAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var query = await BuildScopedCarbonExpenseQueryAsync(currentUserId, startDate, endDate);

            return await query
                .GroupBy(e => e.CarbonEmission!.VehicleType)
                .Select(g => new TransportCostCarbonData
                {
                    TransportType = g.Key,
                    TotalExpense = g.Sum(x => (decimal)x.Amount),
                    TotalCarbon = g.Sum(x => x.CarbonEmission!.TotalCo2)
                })
                .OrderByDescending(x => x.TotalCarbon)
                .ThenByDescending(x => x.TotalExpense)
                .ToListAsync();
        }

        public async Task<EmployeeTravelDetailResponse> GetEmployeeTravelDetailAsync(string currentUserId, string employeeId, DateTime startDate, DateTime endDate)
        {
            var detailQuery = await BuildScopedCarbonExpenseQueryAsync(currentUserId, startDate, endDate);
            var dailyTripQuery = await BuildScopedDailyTripQueryAsync(currentUserId, startDate, endDate);

            var items = await detailQuery
                .Where(e => e.EmployeeId == employeeId)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Select(e => new EmployeeTravelDetailData
                {
                    DailyTripId = e.DailyTripId,
                    Date = e.Date,
                    TransportType = e.CarbonEmission!.VehicleType,
                    DistanceKm = e.CarbonEmission.DistanceKm,
                    Amount = e.Amount,
                    Carbon = e.CarbonEmission.TotalCo2
                })
                .ToListAsync();

            var summary = new EmployeeTravelSummaryData
            {
                TransportationCost = items.Sum(x => x.Amount),
                TotalCarbon = items.Sum(x => x.Carbon),
                TravelDays = await dailyTripQuery
                    .Where(t => t.EmployeeId == employeeId)
                    .Select(t => t.Id)
                    .Distinct()
                    .CountAsync()
            };

            return new EmployeeTravelDetailResponse
            {
                Summary = summary,
                Items = items
            };
        }

        public async Task<string?> GetScopedEmployeeNameAsync(string currentUserId, string employeeId)
        {
            var departmentScope = await GetDepartmentScopeAsync(currentUserId);

            return await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == employeeId && u.DepartmentId == departmentScope.DepartmentId)
                .Select(u => u.Name)
                .SingleOrDefaultAsync();
        }

        private async Task<IQueryable<ExpenseRecord>> BuildScopedExpenseQueryAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var departmentScope = await GetDepartmentScopeAsync(currentUserId);
            var approvedStatusId = await GetApprovedStatusIdAsync();
            var start = startDate.Date;
            var end = endDate.Date;

            return _db.ExpenseRecords
                .AsNoTracking()
                .Where(e => e.Date >= start
                    && e.Date <= end
                    && e.DailyTrip != null
                    && e.DailyTrip.Application != null
                    && e.DailyTrip.Application.StatusId == approvedStatusId
                    && e.DepartmentId == departmentScope.DepartmentId);
        }

        private async Task<IQueryable<ExpenseRecord>> BuildScopedCarbonExpenseQueryAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var query = await BuildScopedExpenseQueryAsync(currentUserId, startDate, endDate);
            return query.Where(e => e.CarbonEmission != null);
        }

        private async Task<IQueryable<DailyTrip>> BuildScopedDailyTripQueryAsync(string currentUserId, DateTime startDate, DateTime endDate)
        {
            var departmentScope = await GetDepartmentScopeAsync(currentUserId);
            var approvedStatusId = await GetApprovedStatusIdAsync();
            var start = startDate.Date;
            var end = endDate.Date;

            return _db.DailyTrips
                .AsNoTracking()
                .Where(t => t.Date >= start
                    && t.Date <= end
                    && t.Application != null
                    && t.Application.StatusId == approvedStatusId
                    && t.DepartmentId == departmentScope.DepartmentId);
        }

        private async Task<DepartmentScope> GetDepartmentScopeAsync(string currentUserId)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId && u.DepartmentId.HasValue)
                .Select(u => new DepartmentScope
                {
                    DepartmentId = u.DepartmentId!.Value
                })
                .SingleOrDefaultAsync()
                ?? throw new InvalidOperationException("目前登入帳號未設定部門，無法載入部門分析資料。");
        }

        private async Task<int> GetApprovedStatusIdAsync()
        {
            var status = await _db.ApplicationStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == "Approved");

            return status?.Id ?? throw new InvalidOperationException("找不到 Approved 狀態");
        }

        private sealed class DepartmentScope
        {
            public int DepartmentId { get; set; }
        }
    }
}
