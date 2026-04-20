namespace StarterM.ViewModels
{
    public class EmissionFactorListItemViewModel
    {
        public int VehicleTypeId { get; set; }

        public string VehicleTypeName { get; set; } = string.Empty;

        public int? EmissionFactorId { get; set; }

        public decimal? Co2PerKm { get; set; }

        public string? Source { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedByName { get; set; }
    }
}