using Microsoft.AspNetCore.Identity;

namespace StarterM.Services
{
    public class TraditionalChineseIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
            => new() { Code = nameof(DefaultError), Description = "發生未知錯誤。" };

        public override IdentityError ConcurrencyFailure()
            => new() { Code = nameof(ConcurrencyFailure), Description = "資料已被其他操作更新，請重新整理後再試一次。" };

        public override IdentityError PasswordMismatch()
            => new() { Code = nameof(PasswordMismatch), Description = "密碼不正確。" };

        public override IdentityError InvalidToken()
            => new() { Code = nameof(InvalidToken), Description = "驗證權杖無效。" };

        public override IdentityError LoginAlreadyAssociated()
            => new() { Code = nameof(LoginAlreadyAssociated), Description = "此登入方式已綁定其他帳號。" };

        public override IdentityError InvalidUserName(string? userName)
            => new() { Code = nameof(InvalidUserName), Description = $"使用者名稱「{userName}」格式無效，只能包含字母或數字。" };

        public override IdentityError InvalidEmail(string? email)
            => new() { Code = nameof(InvalidEmail), Description = $"電子郵件「{email}」格式無效。" };

        public override IdentityError DuplicateUserName(string userName)
            => new() { Code = nameof(DuplicateUserName), Description = $"使用者名稱「{userName}」已被使用。" };

        public override IdentityError DuplicateEmail(string email)
            => new() { Code = nameof(DuplicateEmail), Description = $"電子郵件「{email}」已被註冊。" };

        public override IdentityError InvalidRoleName(string? role)
            => new() { Code = nameof(InvalidRoleName), Description = $"角色名稱「{role}」格式無效。" };

        public override IdentityError DuplicateRoleName(string role)
            => new() { Code = nameof(DuplicateRoleName), Description = $"角色名稱「{role}」已存在。" };

        public override IdentityError UserAlreadyHasPassword()
            => new() { Code = nameof(UserAlreadyHasPassword), Description = "此使用者已設定密碼。" };

        public override IdentityError UserLockoutNotEnabled()
            => new() { Code = nameof(UserLockoutNotEnabled), Description = "此使用者未啟用鎖定功能。" };

        public override IdentityError UserAlreadyInRole(string role)
            => new() { Code = nameof(UserAlreadyInRole), Description = $"此使用者已屬於角色「{role}」。" };

        public override IdentityError UserNotInRole(string role)
            => new() { Code = nameof(UserNotInRole), Description = $"此使用者不屬於角色「{role}」。" };

        public override IdentityError PasswordTooShort(int length)
            => new() { Code = nameof(PasswordTooShort), Description = $"密碼長度至少需要 {length} 個字元。" };

        public override IdentityError PasswordRequiresNonAlphanumeric()
            => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "密碼至少需要包含一個特殊符號。" };

        public override IdentityError PasswordRequiresDigit()
            => new() { Code = nameof(PasswordRequiresDigit), Description = "密碼至少需要包含一個數字。" };

        public override IdentityError PasswordRequiresLower()
            => new() { Code = nameof(PasswordRequiresLower), Description = "密碼至少需要包含一個小寫英文字母。" };

        public override IdentityError PasswordRequiresUpper()
            => new() { Code = nameof(PasswordRequiresUpper), Description = "密碼至少需要包含一個大寫英文字母。" };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
            => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"密碼至少需要包含 {uniqueChars} 個不同字元。" };

        public override IdentityError RecoveryCodeRedemptionFailed()
            => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "復原碼驗證失敗。" };
    }
}