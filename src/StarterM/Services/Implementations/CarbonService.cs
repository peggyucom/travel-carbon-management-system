using Microsoft.EntityFrameworkCore;
using StarterM.Data;
using StarterM.Models;
using StarterM.Services.Interfaces;

namespace StarterM.Services.Implementations
{
    public class CarbonService : ICarbonService
    {
        private readonly ApplicationDbContext _db;

        public CarbonService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CarbonEmissionRecord?> CalculateAndSaveAsync(ExpenseRecord expense)
        {
            if (!expense.DistanceKm.HasValue || expense.DistanceKm <= 0)
                return null;

            var vehicleType = await _db.ExpenseItemVehicleTypeMappings
                .Where(m => m.ExpenseItemId == expense.ExpenseItemId
                    && m.ExpenseItem.IsActive
                    && m.VehicleType.IsActive)
                .Select(m => new
                {
                    m.VehicleTypeId,
                    VehicleTypeName = m.VehicleType.Name
                })
                .FirstOrDefaultAsync();
            if (vehicleType == null) return null;

            var expenseDate = expense.Date.Date;

            // 依交通工具種類查找符合費用日期的最新碳排係數版本
            var factor = await _db.EmissionFactors
                .Where(f => f.VehicleTypeId == vehicleType.VehicleTypeId && f.EffectiveFrom <= expenseDate)
                .Select(f => new
                {
                    Id = (int?)f.Id,
                    f.Co2PerKm,
                    VehicleType = f.VehicleType.Name,
                    f.EffectiveFrom,
                    UpdatedAt = f.UpdatedAt ?? DateTime.MinValue
                })
                .Concat(
                    _db.EmissionFactorHistories
                        .Where(h => h.EmissionFactor != null
                            && h.EmissionFactor.VehicleTypeId == vehicleType.VehicleTypeId
                            && h.EffectiveFrom <= expenseDate)
                        .Select(h => new
                        {
                            Id = (int?)h.EmissionFactorId,
                            h.Co2PerKm,
                            VehicleType = h.VehicleType,
                            h.EffectiveFrom,
                            h.UpdatedAt
                        }))
                .OrderByDescending(f => f.EffectiveFrom)
                .ThenByDescending(f => f.UpdatedAt)
                .ThenByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            if (factor == null) return null;

            var totalCo2 = expense.DistanceKm.Value * factor.Co2PerKm;

            var record = new CarbonEmissionRecord
            {
                ExpenseId = expense.Id,
                VehicleType = factor.VehicleType,
                DistanceKm = expense.DistanceKm.Value,
                Co2PerKm = factor.Co2PerKm,
                TotalCo2 = totalCo2,
                EmissionFactorId = factor.Id
            };

            _db.CarbonEmissionRecords.Add(record);
            await _db.SaveChangesAsync();

            return record;
        }

        public async Task<decimal> GetTotalCarbonByEmployeeAsync(string employeeId, int year)
        {
            var approvedStatusId = await GetApprovedStatusIdAsync();
            return await GetApprovedCarbonRecordsQuery(approvedStatusId)
                .Where(c => c.Expense.EmployeeId == employeeId
                    && c.Expense.Date.Year == year)
                .SumAsync(c => c.TotalCo2);
        }

        public async Task<List<MonthlyCarbonData>> GetMonthlyCarbonAsync(int year)
        {
            var approvedStatusId = await GetApprovedStatusIdAsync();
            var query = await GetApprovedCarbonRecordsQuery(approvedStatusId)
                .Where(c => c.Expense.Date.Year == year)
                .GroupBy(c => c.Expense.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalCo2 = g.Sum(x => x.TotalCo2)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            return query.Select(x => new MonthlyCarbonData
            {
                Month = x.Month.ToString("D2"),
                TotalCo2 = x.TotalCo2
            }).ToList();
        }

        public async Task<List<VehicleCarbonData>> GetCarbonByVehicleTypeAsync(string yearMonth)
        {
            var year = int.Parse(yearMonth.Split('-')[0]);
            var month = int.Parse(yearMonth.Split('-')[1]);
            var approvedStatusId = await GetApprovedStatusIdAsync();

            return await GetApprovedCarbonRecordsQuery(approvedStatusId)
                .Where(c => c.Expense.Date.Year == year
                    && c.Expense.Date.Month == month)
                .GroupBy(c => c.VehicleType)
                .Select(g => new VehicleCarbonData
                {
                    VehicleType = g.Key,
                    TotalCo2 = g.Sum(x => x.TotalCo2)
                })
                .OrderByDescending(x => x.TotalCo2)
                .ToListAsync();
        }

        private async Task<int> GetApprovedStatusIdAsync()
        {
            var status = await _db.ApplicationStatuses.FirstOrDefaultAsync(s => s.Code == "Approved");
            return status?.Id ?? throw new InvalidOperationException("找不到 Approved 狀態");
        }

        private IQueryable<CarbonEmissionRecord> GetApprovedCarbonRecordsQuery(int approvedStatusId)
        {
            return _db.CarbonEmissionRecords.Where(c => c.Expense.DailyTrip != null
                && c.Expense.DailyTrip.Application != null
                && c.Expense.DailyTrip.Application.StatusId == approvedStatusId);
        }
    }
}
