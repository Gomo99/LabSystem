using Microsoft.AspNetCore.Mvc;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    public class LaboratoryManager : Controller
    {
        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
