using System.ComponentModel.DataAnnotations;
using MyDevOpsApp.Data.Entities;

namespace MyDevOpsApp.Areas.Admin.Models;

public class PayrollUploadViewModel
{
    [Required(ErrorMessage = "يرجى تحديد السنة")]
    [Display(Name = "السنة")]
    public int Year { get; set; } = DateTime.Now.Year;

    [Required(ErrorMessage = "يرجى تحديد الشهر")]
    [Range(1, 12, ErrorMessage = "الشهر يجب أن يكون بين 1 و 12")]
    [Display(Name = "الشهر")]
    public int Month { get; set; } = DateTime.Now.Month;

    [Required(ErrorMessage = "يرجى تحديد نوع الملف")]
    [Display(Name = "نوع الملف")]
    public string PayrollType { get; set; } = "مرتبات"; // "مرتبات" أو "حوافز"

    [Display(Name = "ملف الإكسيل")]
    public IFormFile? ExcelFile { get; set; }

    [Display(Name = "ملفات الإكسيل")]
    public List<IFormFile> ExcelFiles { get; set; } = new();

    [Display(Name = "طريقة الإدراج")]
    public bool AppendToExisting { get; set; } = false;
}

public class PayrollReportViewModel
{
    public int SelectedYear { get; set; } = DateTime.Now.Year;
    public int SelectedMonth { get; set; } = DateTime.Now.Month;
    public string SelectedPayrollType { get; set; } = "مرتبات";
    public string? SearchQuery { get; set; }

    public PayrollBatch? CurrentBatch { get; set; }
    public List<PayrollBatch> AllBatches { get; set; } = new();
    public List<PayrollRecord> FilteredRecords { get; set; } = new();

    public decimal TotalEarnings => CurrentBatch?.TotalEarningsSum ?? 0;
    public decimal TotalDeductions => CurrentBatch?.TotalDeductionsSum ?? 0;
    public decimal TotalNet => CurrentBatch?.TotalNetSum ?? 0;
    public int TotalEmployeesCount => CurrentBatch?.TotalEmployeesCount ?? 0;
}
