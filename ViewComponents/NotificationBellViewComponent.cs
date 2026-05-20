using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using LaboratoryTestRequestManagementSystem.Services;

namespace LaboratoryTestRequestManagementSystem.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationBellViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult>
    InvokeAsync()
        {
            if (!(User is ClaimsPrincipal claimsPrincipal) || !claimsPrincipal.Identity.IsAuthenticated)
                return Content(string.Empty);

            var userId = int.Parse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier));
            var userType = claimsPrincipal.FindFirstValue(ClaimTypes.Role);
            var count = await _notificationService.GetUnreadCountAsync(userId, userType);
            return View(count);
        }
    }
}
