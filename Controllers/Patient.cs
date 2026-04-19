using Microsoft.AspNetCore.Mvc;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    public class Patient : Controller
    {
        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
