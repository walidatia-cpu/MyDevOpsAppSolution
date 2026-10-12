using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Data;
using MyDevOpsApp.Models;

namespace MyDevOpsApp.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    return RedirectToAction("Report", "Payroll", new { area = "Admin" });
                }
                if (await _userManager.IsInRoleAsync(currentUser, "Employee"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Employees" });
                }
            }
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("تم تسجيل دخول المستخدم {Email} بنجاح.", model.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Report", "Payroll", new { area = "Admin" });
            }
            if (await _userManager.IsInRoleAsync(user, "Employee"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Employees" });
            }

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        return View(model);
    }

    // شاشة تسجيل حساب موظف جديد وربطه بقاعدة البيانات عبر الرقم القومي ورقم الملف
    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterEmployee()
    {
        return View(new RegisterEmployeeViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterEmployee(RegisterEmployeeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var nationalId = model.NationalId.Trim();
        var paymentNumber = model.PaymentNumber.Trim();

        // 1. التحقق من وجود الموظف في جدول الموظفين بناءً على الرقم القومي ورقم الملف
        var employee = await _context.Employees.FirstOrDefaultAsync(e =>
            e.NationalId == nationalId && e.PaymentNumber == paymentNumber);

        if (employee == null)
        {
            ModelState.AddModelError(string.Empty,
                "عذراً، لم يتم العثور على موظف مسجل بالرقم القومي ورقم الملف المدخلين. يرجى التأكد من البيانات أو مراجعة إدارة المرتبات لرفع مسير الرواتب أولاً.");
            return View(model);
        }

        // 2. التحقق إن كان الموظف مسجل له حساب مسبقاً
        if (!string.IsNullOrEmpty(employee.UserId))
        {
            var existingLinkedUser = await _userManager.FindByIdAsync(employee.UserId);
            if (existingLinkedUser != null)
            {
                ModelState.AddModelError(string.Empty,
                    $"هذا الموظف ({employee.Name}) لديه حساب مسجل مسبقاً بالبريد: {existingLinkedUser.Email}. يمكنك تسجيل الدخول به مباشرة.");
                return View(model);
            }
        }

        // 3. التحقق من عدم تكرار البريد الإلكتروني
        var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (userWithSameEmail != null)
        {
            ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم بالفعل لحساب آخر.");
            return View(model);
        }

        // 4. إنشاء حساب المستخدم في Identity وربطه بالموظف
        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = employee.Name,
            Department = employee.Department ?? "",
            NationalId = employee.NationalId,
            PaymentNumber = employee.PaymentNumber,
            EmployeeId = employee.Id,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var err in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            return View(model);
        }

        // إضافة دور الموظف
        await _userManager.AddToRoleAsync(user, "Employee");

        // ربط معرف المستخدم بسجل الموظف
        employee.UserId = user.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("تم إنشاء وربط حساب الموظف {Name} بنجاح.", employee.Name);

        // تسجيل الدخول التلقائي
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["WelcomeMessage"] = $"أهلاً بك يا {employee.Name}! تم تفعيل حسابك وربطه ببياناتك المالية بنجاح.";
        return RedirectToAction("Index", "Dashboard", new { area = "Employees" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("تم تسجيل الخروج بنجاح.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> LogoutGet()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
