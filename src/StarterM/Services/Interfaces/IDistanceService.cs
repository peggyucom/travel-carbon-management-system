using StarterM.ViewModels;

namespace StarterM.Services.Interfaces
{
    public interface IDistanceService
    {
        Task<decimal?> CalculateDistanceAsync(string origin, string destination);
        Task<DistanceGeocodeResultViewModel?> GeocodeAsync(string address);
        Task<DistanceRouteResultViewModel?> CalculateRouteAsync(DistanceRouteRequestViewModel request);
    }
}
