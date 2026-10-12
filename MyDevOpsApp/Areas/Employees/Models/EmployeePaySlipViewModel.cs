using MyDevOpsApp.Data.Entities;

namespace MyDevOpsApp.Areas.Employees.Models;

public class EmployeePaySlipViewModel
{
    public int SelectedYear { get; set; } = DateTime.Now.Year;
    public int SelectedMonth { get; set; } = DateTime.Now.Month;
    public string SelectedPayrollType { get; set; } = "مرتبات"; // "مرتبات" أو "حوافز"

    public Employee? EmployeeInfo { get; set; }
    public PayrollRecord? CurrentRecord { get; set; }
    public List<EmployeeBatchSummary> AvailableBatches { get; set; } = new();
}

public class EmployeeBatchSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string PayrollType { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
}
