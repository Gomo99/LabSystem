using Microsoft.AspNetCore.Mvc;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    public class LabTechnician : Controller
    {
        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
