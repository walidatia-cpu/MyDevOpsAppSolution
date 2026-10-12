using System.ComponentModel.DataAnnotations;

namespace MyDevOpsApp.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "يرجى إدخال البريد الإلكتروني")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "يرجى إدخال كلمة المرور")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "تذكرني")]
    public bool RememberMe { get; set; }
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
