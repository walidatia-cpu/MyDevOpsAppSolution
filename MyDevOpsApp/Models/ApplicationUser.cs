using Microsoft.AspNetCore.Identity;

namespace MyDevOpsApp.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // الربط مع الموظف في النظام عبر الرقم القومي ورقم الصرف
    public string? NationalId { get; set; }
    public string? PaymentNumber { get; set; }
    public int? EmployeeId { get; set; }
}
