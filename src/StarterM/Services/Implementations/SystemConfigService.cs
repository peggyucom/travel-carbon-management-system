using Microsoft.EntityFrameworkCore;
using StarterM.Data;
using StarterM.Models;
using StarterM.Services.Interfaces;

namespace StarterM.Services.Implementations
{
    public class SystemConfigService : ISystemConfigService
    {
        private const string MealRateKey = "MealAllowanceDailyRate";
        private const string CarRateKey  = "CarAllowancePerKm";
        private const decimal DefaultMealAllowanceDailyRate = 300m;
        private const decimal DefaultCarAllowancePerKm = 5.0m;
        private readonly ApplicationDbContext _db;

        public SystemConfigService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ─── 膳雜費 ────────────────────────────────────────────────────────

        public async Task<decimal> GetMealAllowanceDailyRateAsync()
        {
            var config = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == MealRateKey);
            return config != null && decimal.TryParse(config.Value, out var rate) ? rate : DefaultMealAllowanceDailyRate;
        }

        public async Task<decimal> GetMealAllowanceDailyRateAsync(DateTime tripDate)
        {
            var effectiveDate = tripDate.Date;

            var rate = await _db.MealAllowanceHistories
                .Where(h => h.EffectiveFrom <= effectiveDate)
                .OrderByDescending(h => h.EffectiveFrom)
                .ThenByDescending(h => h.UpdatedAt)
                .ThenByDescending(h => h.Id)
                .Select(h => (decimal?)h.Rate)
                .FirstOrDefaultAsync();

            return rate ?? DefaultMealAllowanceDailyRate;
        }

        public async Task SetMealAllowanceDailyRateAsync(decimal rate, string updatedById, DateTime effectiveFrom)
        {
            var config = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == MealRateKey);

            _db.MealAllowanceHistories.Add(new MealAllowanceHistory
            {
                Rate = rate,
                EffectiveFrom = effectiveFrom.Date,
                UpdatedAt = DateTime.UtcNow,
                UpdatedById = updatedById
            });

            if (config == null)
            {
                config = new SystemConfig { Key = MealRateKey, Description = "膳雜費每日費率（元）" };
                _db.SystemConfigs.Add(config);
            }
            config.Value = rate.ToString("F0");
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedById = updatedById;
            await _db.SaveChangesAsync();
        }

        // ─── 自用車補助 ────────────────────────────────────────────────────

        public async Task<decimal> GetCarAllowancePerKmAsync()
        {
            var config = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == CarRateKey);
            return config != null && decimal.TryParse(config.Value, out var rate) ? rate : DefaultCarAllowancePerKm;
        }

        public async Task<decimal> GetCarAllowancePerKmAsync(DateTime tripDate)
        {
            var effectiveDate = tripDate.Date;

            var rate = await _db.CarAllowanceHistories
                .Where(h => h.EffectiveFrom <= effectiveDate)
                .OrderByDescending(h => h.EffectiveFrom)
                .ThenByDescending(h => h.UpdatedAt)
                .ThenByDescending(h => h.Id)
                .Select(h => (decimal?)h.RatePerKm)
                .FirstOrDefaultAsync();

            return rate ?? DefaultCarAllowancePerKm;
        }

        public async Task SetCarAllowancePerKmAsync(decimal rate, string updatedById, DateTime effectiveFrom)
        {
            var config = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == CarRateKey);

            _db.CarAllowanceHistories.Add(new CarAllowanceHistory
            {
                RatePerKm = rate,
                EffectiveFrom = effectiveFrom.Date,
                UpdatedAt = DateTime.UtcNow,
                UpdatedById = updatedById
            });

            if (config == null)
            {
                config = new SystemConfig { Key = CarRateKey, Description = "自用車補助每公里費率（元）" };
                _db.SystemConfigs.Add(config);
            }
            config.Value = rate.ToString("F1");
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedById = updatedById;
            await _db.SaveChangesAsync();
        }

        // ─── 碳排係數 ──────────────────────────────────────────────────────

        public async Task CreateEmissionFactorAsync(int vehicleTypeId, decimal co2PerKm, string source, DateTime effectiveFrom, string updatedById)
        {
            var vehicleType = await _db.VehicleTypes
                .FirstOrDefaultAsync(v => v.Id == vehicleTypeId && v.IsActive);
            if (vehicleType == null)
                throw new InvalidOperationException("找不到對應的交通工具種類");

            _db.EmissionFactors.Add(new EmissionFactor
            {
                VehicleTypeId = vehicleType.Id,
                Co2PerKm = co2PerKm,
                EffectiveFrom = effectiveFrom.Date,
                Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim(),
                UpdatedAt = DateTime.UtcNow,
                UpdatedById = updatedById
            });

            await _db.SaveChangesAsync();
        }
    }
}
