using System.ComponentModel.DataAnnotations;

namespace MyDevOpsApp.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "يرجى إدخال الرقم القومي أو البريد الإلكتروني")]
    [Display(Name = "الرقم القومي أو البريد الإلكتروني")]
    public string LoginIdentifier { get; set; } = string.Empty;

    // توافقية عكسية مع الكود والاختبارات السابقة
    public string Email
    {
        get => LoginIdentifier;
        set => LoginIdentifier = value;
    }

    [Required(ErrorMessage = "يرجى إدخال كلمة المرور")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "تذكرني")]
    public bool RememberMe { get; set; }
}

public class LoginWith2faViewModel
{
    [Required(ErrorMessage = "يرجى إدخال رمز التحقق")]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "رمز التحقق مكون من 6 أرقام.")]
    [Display(Name = "رمز التحقق الثنائي")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Display(Name = "تذكر هذا الجهاز")]
    public bool RememberMachine { get; set; }

    [Display(Name = "تذكر تسجيل الدخول")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class LoginWithRecoveryCodeViewModel
{
    [Required(ErrorMessage = "يرجى إدخال كود الاسترداد للطوارئ")]
    [Display(Name = "كود الاسترداد")]
    public string RecoveryCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class Manage2faViewModel
{
    public bool Is2faEnabled { get; set; }
    public bool HasAuthenticator { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public string? StatusMessage { get; set; }
}

public class EnableAuthenticatorViewModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;

    [Required(ErrorMessage = "يرجى إدخال كود التحقق من تطبيق المصادقة")]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "الكود مكون من 6 أرقام.")]
    [Display(Name = "كود التحقق")]
    public string Code { get; set; } = string.Empty;
}

public class ShowRecoveryCodesViewModel
{
    public string[] RecoveryCodes { get; set; } = Array.Empty<string>();
}

public class RegisterEmployeeViewModel
{
    [Required(ErrorMessage = "يرجى إدخال الرقم القومي")]
    [Display(Name = "الرقم القومي")]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "يرجى إدخال رقم الملف / رقم الصرف")]
    [Display(Name = "رقم الملف / الصرف")]
    public string PaymentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "يرجى إدخال البريد الإلكتروني")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "يرجى إدخال كلمة المرور")]
    [StringLength(100, ErrorMessage = "يجب ألا تقل كلمة المرور عن {2} أحرف.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور")]
    [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
