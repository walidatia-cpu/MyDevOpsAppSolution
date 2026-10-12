using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Areas.Admin.Models;
using MyDevOpsApp.Data;
using MyDevOpsApp.Services;

namespace MyDevOpsApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PayrollController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IExcelPayrollParserService _parserService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        ApplicationDbContext context,
        IExcelPayrollParserService parserService,
        ILogger<PayrollController> logger)
    {
        _context = context;
        _parserService = parserService;
        _logger = logger;
    }

    // 1. شاشة رفع ملفات المرتبات والحوافز
    [HttpGet]
    public IActionResult Upload()
    {
        var model = new PayrollUploadViewModel
        {
            Year = DateTime.Now.Year,
            Month = DateTime.Now.Month,
            PayrollType = "مرتبات"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(PayrollUploadViewModel model)
    {
        if (model.ExcelFile == null || model.ExcelFile.Length == 0)
        {
            ModelState.AddModelError("ExcelFile", "يرجى اختيار ملف الإكسيل بصيغة .xlsx");
            return View(model);
        }

        var extension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            ModelState.AddModelError("ExcelFile", "الملف المسموح به هو ملفات Excel بصيغة .xlsx فقط.");
            return View(model);
        }

        try
        {
            using var stream = model.ExcelFile.OpenReadStream();
            var batch = await _parserService.ImportPayrollAsync(
                stream,
                model.ExcelFile.FileName,
                model.Year,
                model.Month,
                model.PayrollType);

            TempData["SuccessMessage"] = $"تم بنجاح رفع واستيراد بيانات {model.PayrollType} لشهر {model.Month}/{model.Year} بعدد {batch.TotalEmployeesCount} موظف وإجمالي صافي: {batch.TotalNetSum:N2} ج.م";

            return RedirectToAction(nameof(Report), new { year = model.Year, month = model.Month, type = model.PayrollType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "حدث خطأ أثناء معالجة ملف المرتبات.");
            ModelState.AddModelError(string.Empty, "فشلت معالجة الملف: " + ex.Message);
            return View(model);
        }
    }

    // 1.1 معالجة الرفع التفاعلي مع شريط التقدم الفوري (AJAX Processing)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAjax([FromForm] PayrollUploadViewModel model)
    {
        if (model.ExcelFile == null || model.ExcelFile.Length == 0)
        {
            return Json(new { success = false, message = "يرجى اختيار ملف الإكسيل بصيغة .xlsx" });
        }

        var extension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            return Json(new { success = false, message = "الملف المسموح به هو ملفات Excel بصيغة .xlsx فقط." });
        }

        try
        {
            using var stream = model.ExcelFile.OpenReadStream();
            var batch = await _parserService.ImportPayrollAsync(
                stream,
                model.ExcelFile.FileName,
                model.Year,
                model.Month,
                model.PayrollType);

            string successMsg = $"تم بنجاح استيراد بيانات {model.PayrollType} لشهر {model.Month}/{model.Year} بعدد {batch.TotalEmployeesCount} موظف وإجمالي صافي: {batch.TotalNetSum:N2} ج.م";
            TempData["SuccessMessage"] = successMsg;

            return Json(new
            {
                success = true,
                message = successMsg,
                count = batch.TotalEmployeesCount,
                totalNet = batch.TotalNetSum.ToString("N2"),
                redirectUrl = Url.Action(nameof(Report), new { year = model.Year, month = model.Month, type = model.PayrollType })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "حدث خطأ أثناء معالجة الملف عبر AJAX.");
            return Json(new { success = false, message = "فشلت معالجة الملف: " + ex.Message });
        }
    }

    // 2. شاشة تقرير المرتبات والحوافز الشهرية
    [HttpGet]
    public async Task<IActionResult> Report(int? year, int? month, string? type, string? search)
    {
        int targetYear = year ?? DateTime.Now.Year;
        int targetMonth = month ?? DateTime.Now.Month;
        string targetType = string.IsNullOrEmpty(type) ? "مرتبات" : type;

        var allBatches = await _context.PayrollBatches
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync();

        var currentBatch = await _context.PayrollBatches
            .Include(b => b.Records)
            .FirstOrDefaultAsync(b => b.Year == targetYear && b.Month == targetMonth && b.PayrollType == targetType);

        var records = currentBatch?.Records.AsEnumerable() ?? Enumerable.Empty<Data.Entities.PayrollRecord>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            records = records.Where(r =>
                r.EmployeeName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.NationalId.Contains(s) ||
                r.PaymentNumber.Contains(s) ||
                r.Department.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var viewModel = new PayrollReportViewModel
        {
            SelectedYear = targetYear,
            SelectedMonth = targetMonth,
            SelectedPayrollType = targetType,
            SearchQuery = search,
            CurrentBatch = currentBatch,
            AllBatches = allBatches,
            FilteredRecords = records.ToList()
        };

        return View(viewModel);
    }

    // 3. شاشة تفاصيل قسيمة صرف الموظف (كل بنود الاستحقاقات والاستقطاعات)
    [HttpGet]
    public async Task<IActionResult> RecordDetails(int id)
    {
        var record = await _context.PayrollRecords
            .Include(r => r.PayrollBatch)
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null)
        {
            return NotFound("لم يتم العثور على سجل الصرفية المطلوب.");
        }

        return View(record);
    }

    // 4. سجل الدفعات المرفوعة
    [HttpGet]
    public async Task<IActionResult> Batches()
    {
        var batches = await _context.PayrollBatches
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync();

        return View(batches);
    }

    // 5. حذف بيانات شهر معين بالكامل (مرتبات أو حوافز أو كلاهما)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMonthData(int year, int month, string? type)
    {
        var batchQuery = _context.PayrollBatches
            .Where(b => b.Year == year && b.Month == month);

        if (!string.IsNullOrEmpty(type))
        {
            batchQuery = batchQuery.Where(b => b.PayrollType == type);
        }

        var batchIds = await batchQuery.Select(b => b.Id).ToListAsync();

        if (batchIds.Any())
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var batchId in batchIds)
                {
                    // مسح بنود المفردات أولاً
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM PayrollItems WHERE PayrollRecordId IN (SELECT Id FROM PayrollRecords WHERE PayrollBatchId = {0})", batchId);

                    // مسح سجلات الموظفين ثانياً
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM PayrollRecords WHERE PayrollBatchId = {0}", batchId);

                    // مسح الدفعة ثالثاً
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM PayrollBatches WHERE Id = {0}", batchId);
                }

                await transaction.CommitAsync();

                var successMsg = $"تم بنجاح مسح كافة بيانات صرفية ({type ?? "المرتبات والحوافز"}) لشهر {month}/{year} لتمكين إعادة رفعها نظيفة.";
                TempData["SuccessMessage"] = successMsg;

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = true, message = successMsg });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMsg = $"حدث خطأ أثناء محاولة حذف البيانات: {ex.Message}";
                TempData["ErrorMessage"] = errorMsg;

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = errorMsg });
                }
            }
        }
        else
        {
            var notFoundMsg = $"لا توجد بيانات مسجلة لشهر {month}/{year} لحذفها.";
            TempData["ErrorMessage"] = notFoundMsg;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = false, message = notFoundMsg });
            }
        }

        return RedirectToAction(nameof(Report), new { year, month, type });
    }

    // 6. حذف دفعة محددة بالمعرف
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBatch(int id)
    {
        var batch = await _context.PayrollBatches.FindAsync(id);
        if (batch != null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM PayrollItems WHERE PayrollRecordId IN (SELECT Id FROM PayrollRecords WHERE PayrollBatchId = {0})", id);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM PayrollRecords WHERE PayrollBatchId = {0}", id);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM PayrollBatches WHERE Id = {0}", id);

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"تم حذف دفعة {batch.PayrollType} لشهر {batch.Month}/{batch.Year} بنجاح.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"فشل حذف الدفعة: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(Batches));
    }
}
