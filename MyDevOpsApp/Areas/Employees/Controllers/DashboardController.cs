using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Areas.Employees.Models;
using MyDevOpsApp.Data;
using MyDevOpsApp.Data.Entities;
using MyDevOpsApp.Models;

namespace MyDevOpsApp.Areas.Employees.Controllers;

[Area("Employees")]
[Authorize(Roles = "Admin,Employee")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? year, int? month, string? type)
    {
        int targetYear = year ?? DateTime.Now.Year;
        int targetMonth = month ?? DateTime.Now.Month;
        string targetType = string.IsNullOrEmpty(type) ? "مرتبات" : type;

        var currentUser = await _userManager.GetUserAsync(User);
        Employee? employee = null;

        if (currentUser != null)
        {
            // محاولة جلب الموظف بالـ EmployeeId أو بالرقم القومي / رقم الملف
            if (currentUser.EmployeeId.HasValue)
            {
                employee = await _context.Employees.FindAsync(currentUser.EmployeeId.Value);
            }

            if (employee == null && !string.IsNullOrEmpty(currentUser.NationalId))
            {
                employee = await _context.Employees.FirstOrDefaultAsync(e => e.NationalId == currentUser.NationalId);
            }

            if (employee == null && !string.IsNullOrEmpty(currentUser.PaymentNumber))
            {
                employee = await _context.Employees.FirstOrDefaultAsync(e => e.PaymentNumber == currentUser.PaymentNumber);
            }

            // إذا كان المستخدم هو Admin يتصفح البوابة، نعرض أول موظف للتجربة والمعاينة إن وُجد
            if (employee == null && await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                employee = await _context.Employees.FirstOrDefaultAsync();
            }
        }

        var viewModel = new EmployeePaySlipViewModel
        {
            SelectedYear = targetYear,
            SelectedMonth = targetMonth,
            SelectedPayrollType = targetType,
            EmployeeInfo = employee
        };

        if (employee != null)
        {
            // جلب سجلات الموظف لهذا الشهر والنوع مع بنودها
            viewModel.CurrentRecord = await _context.PayrollRecords
                .Include(r => r.PayrollBatch)
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r =>
                    r.EmployeeId == employee.Id &&
                    r.PayrollBatch.Year == targetYear &&
                    r.PayrollBatch.Month == targetMonth &&
                    r.PayrollBatch.PayrollType == targetType);

            // جلب قائمة كافة الأشهر المتوفرة لهذا الموظف
            viewModel.AvailableBatches = await _context.PayrollRecords
                .Where(r => r.EmployeeId == employee.Id)
                .Select(r => new EmployeeBatchSummary
                {
                    Year = r.PayrollBatch.Year,
                    Month = r.PayrollBatch.Month,
                    PayrollType = r.PayrollBatch.PayrollType,
                    NetAmount = r.NetAmount
                })
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .ToListAsync();
        }

        return View(viewModel);
    }
}
