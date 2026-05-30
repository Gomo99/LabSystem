// Services/NotificationService.cs
using LaboratoryTestRequestManagementSystem.AppStatus;
using LaboratoryTestRequestManagementSystem.Data;
using LaboratoryTestRequestManagementSystem.Hubs;
using LaboratoryTestRequestManagementSystem.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaboratoryTestRequestManagementSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly LabDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(LabDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAsync(int userId, string userType, string message, string link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                UserType = userType,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedDate = DateTime.Now,
                Status = Status.Active
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast the new notification to the target user's group
            string groupName = $"{userType}-{userId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                message = notification.Message,
                link = notification.Link,
                createdDate = notification.CreatedDate.ToString("g")
            });

            // Also push the updated unread count
            int unreadCount = await GetUnreadCountAsync(userId, userType);
            await _hubContext.Clients.Group(groupName).SendAsync("UpdateUnreadCount", unreadCount);
        }

        public async Task<int> GetUnreadCountAsync(int userId, string userType)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && !n.IsRead
                            && n.Status == Status.Active)
                .CountAsync();
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId, string userType)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && n.Status == Status.Active)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        // NEW METHOD: Get recent notifications for dropdown
        public async Task<List<Notification>> GetRecentNotificationsAsync(int userId, string userType, int count)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && n.Status == Status.Active)
                .OrderByDescending(n => n.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId, string userType)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = Status.Inactive;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearAllAsync(int userId, string userType)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.UserType == userType && n.Status == Status.Active)
                .ToListAsync();

            foreach (var n in notifications)
                n.Status = Status.Inactive;

            await _context.SaveChangesAsync();
        }
    }
}