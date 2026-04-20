using Microsoft.AspNetCore.Mvc;
using StarterM.Services.Interfaces;
using System.Security.Claims;

namespace StarterM.ViewComponents
{
    public class PendingReviewBadgeViewComponent : ViewComponent
    {
        private readonly IApplicationService _applicationService;

        public PendingReviewBadgeViewComponent(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.User.IsInRole("Manager"))
            {
                var managerId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var count = string.IsNullOrEmpty(managerId)
                    ? 0
                    : await _applicationService.GetPendingReviewCountAsync(managerId);
                return View("Default", count);
            }
            return View("Default", 0);
        }
    }
}
