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

    // ==========================================
    // 1. تسجيل الدخول (يدعم الرقم القومي أو الإيميل)
    // ==========================================
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                return await RedirectToUserArea(currentUser, returnUrl);
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

        var identifier = (model.LoginIdentifier ?? model.Email ?? string.Empty).Trim();
        ApplicationUser? user = null;

        // 1. البحث أولاً بالرقم القومي (سواء 14 رقم أو مطابق)
        if (!string.IsNullOrEmpty(identifier))
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.NationalId == identifier);
        }

        // 2. إذا لم يتم العثور عليه، ابحث بالبريد الإلكتروني أو اسم المستخدم
        if (user == null && !string.IsNullOrEmpty(identifier))
        {
            user = await _userManager.FindByEmailAsync(identifier)
                ?? await _userManager.FindByNameAsync(identifier);
        }

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "الرقم القومي / البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        // التحقق إن كان المستخدم مفعلاً لـ 2FA
        if (result.RequiresTwoFactor)
        {
            _logger.LogInformation("المستخدم {UserName} يتطلب المصادقة الثنائية (2FA).", user.UserName);
            return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, rememberMe = model.RememberMe });
        }

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.Now;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("تم تسجيل دخول المستخدم {UserName} بنجاح.", user.UserName);
            return await RedirectToUserArea(user, returnUrl);
        }

        ModelState.AddModelError(string.Empty, "الرقم القومي / البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        return View(model);
    }

    // ==========================================
    // 2. التحقق بخطوتين (Two-Factor Authentication)
    // ==========================================
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new LoginWith2faViewModel
        {
            RememberMe = rememberMe,
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var cleanCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            cleanCode,
            model.RememberMe,
            model.RememberMachine);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.Now;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("تم التحقق بنجاح بالمصادقة الثنائية للمستخدم {UserName}.", user.UserName);
            return await RedirectToUserArea(user, model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("تم قفل حساب المستخدم {UserName}.", user.UserName);
            ModelState.AddModelError(string.Empty, "تم قفل الحساب مؤقتاً لتكرار المحاولات الخاطئة.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "رمز التحقق الثنائي غير صحيح أو انتهت صلاحيته.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithRecoveryCode(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new LoginWithRecoveryCodeViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.Now;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("تم تسجيل دخول المستخدم {UserName} باستخدام كود الاسترداد.", user.UserName);
            return await RedirectToUserArea(user, model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "كود الاسترداد غير صحيح.");
        return View(model);
    }

    // ==========================================
    // 3. إدارة إعدادات 2FA للمستخدم الحالي
    // ==========================================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Manage2fa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("لم يتم العثور على المستخدم.");
        }

        var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var recoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user);

        var model = new Manage2faViewModel
        {
            Is2faEnabled = is2faEnabled,
            HasAuthenticator = !string.IsNullOrEmpty(key),
            RecoveryCodesLeft = recoveryCodesLeft,
            StatusMessage = TempData["StatusMessage"] as string
        };

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EnableAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("لم يتم العثور على المستخدم.");
        }

        var model = new EnableAuthenticatorViewModel();
        await LoadSharedKeyAndQrCodeUriAsync(user, model);

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("لم يتم العثور على المستخدم.");
        }

        if (!ModelState.IsValid)
        {
            await LoadSharedKeyAndQrCodeUriAsync(user, model);
            return View(model);
        }

        var cleanCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            cleanCode);

        if (!is2faTokenValid)
        {
            ModelState.AddModelError("Code", "كود التحقق غير صحيح أو انتهت صلاحيته.");
            await LoadSharedKeyAndQrCodeUriAsync(user, model);
            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        _logger.LogInformation("قام المستخدم {UserName} بتفعيل المصادقة الثنائية بنجاح.", user.UserName);

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["StatusMessage"] = "تم تفعيل المصادقة الثنائية (2FA) بنجاح!";

        return View("ShowRecoveryCodes", new ShowRecoveryCodesViewModel
        {
            RecoveryCodes = recoveryCodes?.ToArray() ?? Array.Empty<string>()
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2fa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("لم يتم العثور على المستخدم.");
        }

        var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disable2faResult.Succeeded)
        {
            throw new InvalidOperationException("حدث خطأ أثناء تعطيل المصادقة الثنائية.");
        }

        await _userManager.ResetAuthenticatorKeyAsync(user);
        _logger.LogInformation("قام المستخدم {UserName} بتعطيل المصادقة الثنائية.", user.UserName);
        TempData["StatusMessage"] = "تم تعطيل المصادقة الثنائية بنجاح.";

        return RedirectToAction(nameof(Manage2fa));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("لم يتم العثور على المستخدم.");
        }

        var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (!is2faEnabled)
        {
            return RedirectToAction(nameof(Manage2fa));
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["StatusMessage"] = "تم إنشاء أكواد استرداد جديدة للطوارئ.";

        return View("ShowRecoveryCodes", new ShowRecoveryCodesViewModel
        {
            RecoveryCodes = recoveryCodes?.ToArray() ?? Array.Empty<string>()
        });
    }

    // ==========================================
    // 4. تسجيل حساب موظف جديد
    // ==========================================
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

        var employee = await _context.Employees.FirstOrDefaultAsync(e =>
            e.NationalId == nationalId && e.PaymentNumber == paymentNumber);

        if (employee == null)
        {
            ModelState.AddModelError(string.Empty,
                "عذراً، لم يتم العثور على موظف مسجل بالرقم القومي ورقم الملف المدخلين. يرجى التأكد من البيانات أو مراجعة إدارة المرتبات لرفع مسير الرواتب أولاً.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(employee.UserId))
        {
            var existingLinkedUser = await _userManager.FindByIdAsync(employee.UserId);
            if (existingLinkedUser != null)
            {
                ModelState.AddModelError(string.Empty,
                    $"هذا الموظف ({employee.Name}) لديه حساب مسجل مسبقاً بالبريد: {existingLinkedUser.Email} أو بالرقم القومي. يمكنك تسجيل الدخول به مباشرة.");
                return View(model);
            }
        }

        var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (userWithSameEmail != null)
        {
            ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم بالفعل لحساب آخر.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = employee.Name,
            Department = employee.Department ?? "",
            NationalId = employee.NationalId,
            PaymentNumber = employee.PaymentNumber,
            EmployeeId = employee.Id,
            CreatedAt = DateTime.Now,
            LastLoginAt = DateTime.Now,
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

        await _userManager.AddToRoleAsync(user, "Employee");
        employee.UserId = user.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("تم إنشاء وربط حساب الموظف {Name} بنجاح.", employee.Name);
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

    // ==========================================
    // المساعدات الخاصة بالتوجيه والـ 2FA
    // ==========================================
    private async Task<IActionResult> RedirectToUserArea(ApplicationUser user, string? returnUrl = null)
    {
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

    private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user, EnableAuthenticatorViewModel model)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        model.SharedKey = FormatKey(unformattedKey!);
        model.AuthenticatorUri = GenerateQrCodeUri(user.Email ?? user.UserName!, unformattedKey!);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new System.Text.StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }
        return result.ToString().ToUpperInvariant();
    }

    private static string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            System.Text.Encodings.Web.UrlEncoder.Default.Encode("MyDevOpsApp Payroll"),
            System.Text.Encodings.Web.UrlEncoder.Default.Encode(email),
            unformattedKey);
    }
}
