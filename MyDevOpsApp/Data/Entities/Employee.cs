using System.ComponentModel.DataAnnotations;

namespace MyDevOpsApp.Data.Entities;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string NationalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PaymentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? InsuranceNumber { get; set; }

    [MaxLength(100)]
    public string? Grade { get; set; }

    [MaxLength(200)]
    public string? Department { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(100)]
    public string? Branch { get; set; }

    // معرف المستخدم المرتبط في Identity عند قيام الموظف بالتسجيل
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<PayrollRecord> PayrollRecords { get; set; } = new();
}
