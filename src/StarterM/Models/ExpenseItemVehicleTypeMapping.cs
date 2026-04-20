namespace StarterM.Models
{
    public class ExpenseItemVehicleTypeMapping
    {
        public int Id { get; set; }

        public int ExpenseItemId { get; set; }
        public ExpenseItem ExpenseItem { get; set; } = null!;

        public int VehicleTypeId { get; set; }
        public VehicleType VehicleType { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}