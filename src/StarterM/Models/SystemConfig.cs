namespace StarterM.Models
{
    public class SystemConfig
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedById { get; set; }
        public ApplicationUser? UpdatedBy { get; set; }
    }
}
