using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyDevOpsApp.Areas.Employees.Controllers;

[Area("Employees")]
[Authorize(Roles = "Admin,Employee")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Dashboard", new { area = "Employees" });
    }
}
