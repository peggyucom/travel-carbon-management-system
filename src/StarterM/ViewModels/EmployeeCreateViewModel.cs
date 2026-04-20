using System.ComponentModel.DataAnnotations;

namespace StarterM.ViewModels
{
    public class EmployeeCreateViewModel
    {
        [Required(ErrorMessage = "請輸入姓名")]
        [MaxLength(50, ErrorMessage = "姓名不可超過 50 字")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼")]
        [MinLength(6, ErrorMessage = "密碼至少 6 個字元")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
