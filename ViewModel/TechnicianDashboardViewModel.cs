using LaboratoryTestRequestManagementSystem.AppStatus;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TechnicianDashboardViewModel
    {
        public int SelectedTestsCount { get; set; }
        public int WaitingForSelectionCount { get; set; }
        public int WaitingForVerificationCount { get; set; }
        public int WaitingForReviewCount { get; set; }
        public int UrgentTestsCount { get; set; }
        public int OverdueTestsCount { get; set; }
        public int NearingLimitCount { get; set; }

        public List<DashboardTestItemViewModel> SelectedTests { get; set; } = new();
        public List<DashboardTestItemViewModel> WaitingForSelectionTests { get; set; } = new();
        public List<DashboardTestItemViewModel> WaitingForVerificationTests { get; set; } = new();
        public List<DashboardTestItemViewModel> WaitingForReviewTests { get; set; } = new();
        public List<DashboardTestItemViewModel> UrgentTests { get; set; } = new();
        public List<DashboardTestItemViewModel> OverdueTests { get; set; } = new();
        public List<DashboardTestItemViewModel> NearingLimitTests { get; set; } = new();

        public string? FilterUrgency { get; set; }
        public int? FilterCategoryId { get; set; }
        public string? FilterDueTime { get; set; }
        public string? FilterRequestNumber { get; set; }

        public SelectList UrgencyOptions { get; set; } = new(Enum.GetValues<Urgency>().Select(u => new { Value = u.ToString(), Text = u.ToString() }), "Value", "Text");
        public SelectList CategoryOptions { get; set; } = null!;
        public SelectList DueTimeOptions { get; set; } = new(new[]
        {
            new { Value = "", Text = "All" },
            new { Value = "Today", Text = "Due Today" },
            new { Value = "ThisWeek", Text = "Due This Week" },
            new { Value = "Overdue", Text = "Overdue" },
            new { Value = "Nearing", Text = "Nearing Limit (within 30 min)" }
        }, "Value", "Text");
    }
}