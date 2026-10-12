namespace MyDevOpsApp.Areas.Admin.Models;

public class EmployeeManagementModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "نشط";
    public DateTime JoinDate { get; set; }
}
