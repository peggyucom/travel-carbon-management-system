using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterM.Models;
using StarterM.ViewModels;

namespace StarterM.Controllers
{
    public class AccountController : Controller
    {
        //用 ASP.NET Core Identity 做登入/登出的控制器
        //SignInManager<ApplicationUser>：管理登入狀態(登入.登出.驗證密碼.建立Cookie）
        private readonly SignInManager<ApplicationUser> _signInManager;
                
        public AccountController(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        //登入後導向原本網頁
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

       
        [HttpPost]
        //CSRF 防護(跨站請求偽造->400 error)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                //防止 Open Redirect Attack（釣魚跳轉攻擊）
                return LocalRedirect(returnUrl ?? "/");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(nameof(model.Email), "此帳號已被停用，請聯繫管理者");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            //Identity會刪除Authentication Cookie
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        //權限檢查
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
