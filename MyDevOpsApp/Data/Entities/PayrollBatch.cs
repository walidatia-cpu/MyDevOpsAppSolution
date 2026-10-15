using System.ComponentModel.DataAnnotations;

namespace MyDevOpsApp.Data.Entities;

public class PayrollBatch
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    [Required]
    [MaxLength(50)]
    public string PayrollType { get; set; } = "مرتبات"; // "مرتبات" أو "حوافز"

    [MaxLength(500)]
    public string OriginalFileName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public int TotalEmployeesCount { get; set; }

    public decimal TotalEarningsSum { get; set; }

    public decimal TotalDeductionsSum { get; set; }

    public decimal TotalNetSum { get; set; }

    public List<PayrollRecord> Records { get; set; } = new();
}
