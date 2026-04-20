using LaboratoryTestRequestManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaboratoryTestRequestManagementSystem.ViewComponents
{
    public class TestReviewHistoryViewComponent : ViewComponent
    {
        private readonly LabDbContext _context;

        public TestReviewHistoryViewComponent(LabDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int testRequestId, int testTypeId)
        {
            var history = await _context.TestReviewHistories
                .Include(h => h.Reviewer)
                .Where(h => h.TestRequestId == testRequestId && h.TestTypeId == testTypeId)
                .OrderByDescending(h => h.ReviewDate)
                .ToListAsync();

            return View(history);
        }
    }
}