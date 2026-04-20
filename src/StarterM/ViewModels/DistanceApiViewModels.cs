using System.Text.Json;

namespace StarterM.ViewModels
{
    public class DistanceGeocodeResultViewModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class DistanceRouteRequestViewModel
    {
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public double? OriginLatitude { get; set; }
        public double? OriginLongitude { get; set; }
        public double? DestinationLatitude { get; set; }
        public double? DestinationLongitude { get; set; }
        public bool IsRoundTrip { get; set; } = true;
    }

    public class DistanceRouteResultViewModel
    {
        public decimal SingleDistanceKm { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public JsonElement? Geometry { get; set; }
    }
}
