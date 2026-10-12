using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDevOpsApp.Areas.Admin.Models;

namespace MyDevOpsApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class EmployeesController : Controller
{
    private static readonly List<EmployeeManagementModel> EmployeesList = new()
    {
        new() { Id = 1, FullName = "أحمد محمود سالم", Email = "ahmed.salem@company.com", Department = "هندسة البرمجيات", Role = "مهندس DevOps أول", Status = "نشط", JoinDate = new DateTime(2023, 3, 15) },
        new() { Id = 2, FullName = "سارة إبراهيم حسن", Email = "sara.hassan@company.com", Department = "تطوير الواجهات", Role = "مطور واجهات أول", Status = "نشط", JoinDate = new DateTime(2023, 7, 1) },
        new() { Id = 3, FullName = "محمد علي عبد الرحمن", Email = "mohamed.ali@company.com", Department = "إدارة الأنظمة", Role = "مهندس بنية تحتية سحابية", Status = "إجازة", JoinDate = new DateTime(2022, 11, 20) },
        new() { Id = 4, FullName = "نور الدين فتحي", Email = "nour.fathi@company.com", Department = "ضمان الجودة", Role = "مهندس اختبارات أتمتة", Status = "نشط", JoinDate = new DateTime(2024, 1, 10) },
        new() { Id = 5, FullName = "مريم خالد يوسف", Email = "mariam.khaled@company.com", Department = "إدارة المشاريع", Role = "مديرة مشروعات تقنية", Status = "نشط", JoinDate = new DateTime(2021, 5, 4) }
    };

    public IActionResult Index()
    {
        return View(EmployeesList);
    }

    public IActionResult Details(int id)
    {
        var employee = EmployeesList.FirstOrDefault(e => e.Id == id);
        if (employee == null)
        {
            return NotFound();
        }
        return View(employee);
    }
}
