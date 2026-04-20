using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarterM.Models;
using StarterM.Services.Interfaces;

namespace StarterM.Controllers
{
    public class HomeController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IApplicationService applicationService, UserManager<ApplicationUser> userManager)
        {
            _applicationService = applicationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = _userManager.GetUserId(User)!;

            if (User.IsInRole("Manager"))
            {
                var pendingCount = await _applicationService.GetPendingReviewCountAsync(userId);
                ViewData["PendingReviewCount"] = pendingCount;
            }

            if (User.IsInRole("Employee"))
            {
                var applications = await _applicationService.GetByEmployeeIdAsync(userId);
                var rejected = await _applicationService.GetRejectedByEmployeeAsync(userId);
                var today = DateTime.Today;

                var currentMonthApplications = applications
                    .Where(a => a.DailyTrips.Any(d => d.Date.Year == today.Year && d.Date.Month == today.Month))
                    .ToList();

                var currentMonthApprovedCount = applications.Count(a =>
                    a.Status.Code == "Approved"
                    && a.ApprovedAt.HasValue
                    && a.ApprovedAt.Value.ToLocalTime().Year == today.Year
                    && a.ApprovedAt.Value.ToLocalTime().Month == today.Month);

                ViewData["EmployeeApplicationCount"] = currentMonthApplications.Count;
                ViewData["EmployeePendingCount"] = applications.Count(a => a.Status.Code == "Submitted");
                ViewData["EmployeeApprovedCount"] = currentMonthApprovedCount;
                ViewData["RejectedApplications"] = rejected;
            }

            return View();
        }
    }
}
