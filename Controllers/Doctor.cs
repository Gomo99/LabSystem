using Microsoft.AspNetCore.Mvc;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    public class Doctor : Controller
    {
        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
