// Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using LaboratoryTestRequestManagementSystem.Services;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userType = User.FindFirstValue(ClaimTypes.Role);

            var notifications = await _notificationService.GetNotificationsAsync(userId, userType);
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // For AJAX: return unread count
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userType = User.FindFirstValue(ClaimTypes.Role);
            var count = await _notificationService.GetUnreadCountAsync(userId, userType);
            return Json(count);
        }

        // NEW: For dropdown - get recent notifications
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userType = User.FindFirstValue(ClaimTypes.Role);
            var notifications = await _notificationService.GetRecentNotificationsAsync(userId, userType, 5);

            return Json(notifications.Select(n => new
            {
                id = n.Id,
                message = n.Message,
                link = n.Link,
                isRead = n.IsRead,
                createdDate = n.CreatedDate.ToString("g")
            }));
        }

        // NEW: AJAX endpoint for marking as read
        [HttpPost]
        public async Task<IActionResult> MarkAsReadAjax(int id)
        {
            await _notificationService.MarkAsReadAsync(id);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userType = User.FindFirstValue(ClaimTypes.Role);
            var count = await _notificationService.GetUnreadCountAsync(userId, userType);

            return Json(new { success = true, unreadCount = count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userType = User.FindFirstValue(ClaimTypes.Role);
            await _notificationService.MarkAllAsReadAsync(userId, userType);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificationService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userType = User.FindFirstValue(ClaimTypes.Role);
            await _notificationService.ClearAllAsync(userId, userType);
            return RedirectToAction(nameof(Index));
        }
    }
}