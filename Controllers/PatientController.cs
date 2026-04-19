using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaboratoryTestRequestManagementSystem.Controllers
{
    [Authorize(Roles = "Patient")]

    public class PatientController : Controller
    {
        public IActionResult DashBoard()
        {
            return View();
        }
    }
}
