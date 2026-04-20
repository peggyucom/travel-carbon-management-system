using System.ComponentModel.DataAnnotations;

namespace StarterM.ViewModels
{
    public class DailyTripCreateViewModel
    {
        public int? ApplicationId { get; set; }

        [Required(ErrorMessage = "請選擇日期")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "請填寫出差事由")]
        [MaxLength(200, ErrorMessage = "出差事由不可超過 200 字")]
        public string TripReason { get; set; } = string.Empty;
    }
}
