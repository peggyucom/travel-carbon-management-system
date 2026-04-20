namespace StarterM.ViewModels.Dashboard
{
    public class EmployeeTravelDetailPageViewModel
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string View { get; set; } = "cost";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsCarbonFocus => string.Equals(View, "carbon", StringComparison.OrdinalIgnoreCase);
    }
}