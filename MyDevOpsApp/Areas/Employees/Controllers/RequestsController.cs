using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDevOpsApp.Areas.Employees.Models;

namespace MyDevOpsApp.Areas.Employees.Controllers;

[Area("Employees")]
[Authorize(Roles = "Admin,Employee")]
public class RequestsController : Controller
{
    private static readonly List<LeaveRequestModel> RequestsList = new()
    {
        new() { Id = 1, RequestType = "إجازة اعتيادية سنوية", StartDate = "2026-11-01", EndDate = "2026-11-05", Status = "موافق عليها", BadgeClass = "badge-success" },
        new() { Id = 2, RequestType = "إذن انصراف مبكر لظرف طارئ", StartDate = "2026-10-18", EndDate = "2026-10-18", Status = "قيد المراجعة", BadgeClass = "badge-warning" },
        new() { Id = 3, RequestType = "طلب عمل عن بعد (Remote Day)", StartDate = "2026-10-15", EndDate = "2026-10-15", Status = "موافق عليها", BadgeClass = "badge-success" },
        new() { Id = 4, RequestType = "إجازة مرضية", StartDate = "2026-09-02", EndDate = "2026-09-03", Status = "مكتملة", BadgeClass = "badge-info" }
    };

    public IActionResult Index()
    {
        return View(RequestsList);
    }
}
