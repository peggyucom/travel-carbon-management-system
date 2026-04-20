using System.ComponentModel.DataAnnotations;

namespace StarterM.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "請輸入姓名")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入 Email")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "密碼至少 6 個字元")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "請確認密碼")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "兩次輸入的密碼不一致")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public int? DepartmentId { get; set; }
    }
}
