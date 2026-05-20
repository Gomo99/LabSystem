// Models/Notification.cs
using LaboratoryTestRequestManagementSystem.AppStatus;
using System;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }          // ID of the target user
        public string UserType { get; set; }    // "Doctor", "Patient", "LabTechnician", "LaboratoryManager"
        public string Message { get; set; }
        public string Link { get; set; }        // e.g., "/Doctor/RequestDetails/5"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Status Status { get; set; } = Status.Active;
    }
}