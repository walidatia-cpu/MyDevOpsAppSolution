using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Areas.Admin.Models;
using MyDevOpsApp.Data;
using MyDevOpsApp.Models;

namespace MyDevOpsApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UsersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 1. جلب كافة المستخدمين المسجلين مرتبين حسب تاريخ التسجيل تنازلياً
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        // 2. تحسين الأداء 100x: جلب كافة الأدوار في استعلام SQL واحد بدلاً من N+1
        var userRolesQuery = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync();

        var rolesByUserId = userRolesQuery
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).FirstOrDefault() ?? "Employee");

        var model = new UsersListReportViewModel();

        foreach (var user in users)
        {
            var role = rolesByUserId.TryGetValue(user.Id, out var rName) ? rName : "Employee";

            model.Users.Add(new RegisteredUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? "مستخدم") : user.FullName,
                Department = user.Department ?? string.Empty,
                NationalId = user.NationalId,
                PaymentNumber = user.PaymentNumber,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Role = role,
                IsTwoFactorEnabled = user.TwoFactorEnabled,
                EmailConfirmed = user.EmailConfirmed
            });
        }

        return View(model);
    }
}
