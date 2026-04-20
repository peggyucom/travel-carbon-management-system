namespace StarterM.Services.Interfaces
{
    public interface ISystemConfigService
    {
        Task<decimal> GetMealAllowanceDailyRateAsync();
        Task<decimal> GetMealAllowanceDailyRateAsync(DateTime tripDate);
        Task SetMealAllowanceDailyRateAsync(decimal rate, string updatedById, DateTime effectiveFrom);

        Task<decimal> GetCarAllowancePerKmAsync();
        Task<decimal> GetCarAllowancePerKmAsync(DateTime tripDate);
        Task SetCarAllowancePerKmAsync(decimal rate, string updatedById, DateTime effectiveFrom);

        Task CreateEmissionFactorAsync(int vehicleTypeId, decimal co2PerKm, string source, DateTime effectiveFrom, string updatedById);
    }
}
