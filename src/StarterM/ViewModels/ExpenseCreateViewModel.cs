using System.ComponentModel.DataAnnotations;

namespace StarterM.ViewModels
{
    public class ExpenseCreateViewModel
    {
        [Required]
        public int DailyTripId { get; set; }

        [Required(ErrorMessage = "請選擇費用分類")]
        public int ExpenseCategoryId { get; set; }

        [Required(ErrorMessage = "請選擇費用項目")]
        public int ExpenseItemId { get; set; }

        [Required(ErrorMessage = "請輸入金額")]
        [Range(1, 100000, ErrorMessage = "金額需介於 1 ~ 100,000 元")]
        public int Amount { get; set; }

        [Range(0.1, 1000, ErrorMessage = "公里數需介於 0.1 ~ 1,000")]
        public decimal? DistanceKm { get; set; }

        [MaxLength(100, ErrorMessage = "說明不可超過 100 字")]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string? Origin { get; set; }

        [MaxLength(10)]
        public string? Destination { get; set; }

        public double? OriginLatitude { get; set; }
        public double? OriginLongitude { get; set; }
        public double? DestinationLatitude { get; set; }
        public double? DestinationLongitude { get; set; }

        public bool IsRoundTrip { get; set; } = true;
    }
}
