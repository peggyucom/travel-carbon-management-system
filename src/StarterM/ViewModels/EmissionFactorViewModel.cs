using System.ComponentModel.DataAnnotations;

namespace StarterM.ViewModels
{
    public class EmissionFactorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請選擇交通工具")]
        public int VehicleTypeId { get; set; }

        public string VehicleTypeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入排放係數")]
        [Range(0.001, 10, ErrorMessage = "排放係數需介於 0.001 ~ 10")]
        public decimal Co2PerKm { get; set; }

        [Required(ErrorMessage = "請選擇生效日期")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "請輸入資料來源")]
        [MaxLength(200)]
        public string Source { get; set; } = string.Empty;

        public DateTime MinEffectiveFrom { get; set; } = DateTime.Today;

        public DateTime MaxEffectiveFrom { get; set; } = DateTime.Today.AddDays(7);
    }
}
