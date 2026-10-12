using System.ComponentModel.DataAnnotations;

namespace MyDevOpsApp.Data.Entities;

public class PayrollRecord
{
    public int Id { get; set; }

    public int PayrollBatchId { get; set; }
    public PayrollBatch PayrollBatch { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PaymentNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string NationalId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string PaymentType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string BankName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Branch { get; set; } = string.Empty;

    [MaxLength(50)]
    public string InsuranceNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Grade { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Department { get; set; } = string.Empty;

    public decimal TotalEarnings { get; set; }

    public decimal TotalDeductions { get; set; }

    public decimal NetAmount { get; set; }

    public List<PayrollItem> Items { get; set; } = new();
}
