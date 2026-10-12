using System.ComponentModel.DataAnnotations;

namespace MyDevOpsApp.Data.Entities;

public class PayrollItem
{
    public int Id { get; set; }

    public int PayrollRecordId { get; set; }
    public PayrollRecord PayrollRecord { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string ItemType { get; set; } = "Earning"; // "Earning" (استحقاق) أو "Deduction" (استقطاع)

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public decimal? Value { get; set; }

    public decimal? Total { get; set; }
}
