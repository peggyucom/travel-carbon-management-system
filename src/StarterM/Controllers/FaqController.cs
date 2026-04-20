using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterM.Models;
using StarterM.Services.Interfaces;
using StarterM.ViewModels;

namespace StarterM.Controllers
{
    public class FaqController : Controller
    {
        private readonly IFaqService _faqService;

        public FaqController(IFaqService faqService)
        {
            _faqService = faqService;
        }

        public async Task<IActionResult> Index(string? keyword, string? category)
        {
            var faqs = await _faqService.SearchAsync(keyword, category);
            ViewData["Keyword"] = keyword;
            ViewData["Category"] = category;
            return View(faqs);
        }

        // AJAX 搜尋
        [HttpGet]
        [Route("api/faq")]
        public async Task<IActionResult> Search([FromQuery] string? keyword, [FromQuery] string? category)
        {
            var faqs = await _faqService.SearchAsync(keyword, category);
            return Ok(ApiResponse<object>.Ok(faqs));
        }

        // 主管管理 FAQ
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Manage(string? keyword, string? category)
        {
            var faqs = await _faqService.SearchAsync(keyword, category);
            ViewData["Keyword"] = keyword;
            ViewData["Category"] = category;
            return View(faqs);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFaq(Faq faq)
        {
            await _faqService.CreateAsync(faq);
            TempData["Success"] = "FAQ 已新增";
            return RedirectToAction(nameof(Manage));
        }

        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> EditFaq(int id)
        {
            var faq = await _faqService.GetByIdAsync(id);
            if (faq == null) return NotFound();
            return Json(faq);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaq(Faq faq)
        {
            await _faqService.UpdateAsync(faq);
            TempData["Success"] = "FAQ 已更新";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            await _faqService.DeleteAsync(id);
            TempData["Success"] = "FAQ 已刪除";
            return RedirectToAction(nameof(Manage));
        }
    }

}
