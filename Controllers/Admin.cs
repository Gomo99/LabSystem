using Microsoft.AspNetCore.Mvc;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    public class Admin : Controller
    {
        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
