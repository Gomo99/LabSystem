// Services/INotificationService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using LaboratoryTestRequestManagementSystem.Models;

namespace LaboratoryTestRequestManagementSystem.Services
{
    public interface INotificationService
    {
        Task CreateAsync(int userId, string userType, string message, string link = null);
        Task<int> GetUnreadCountAsync(int userId, string userType);
        Task<List<Notification>> GetNotificationsAsync(int userId, string userType);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId, string userType);
        Task DeleteAsync(int notificationId);
        Task ClearAllAsync(int userId, string userType);
    }
}