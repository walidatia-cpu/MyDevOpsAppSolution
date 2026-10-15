using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Areas.Admin.Models;
using MyDevOpsApp.Data;
using MyDevOpsApp.Data.Entities;
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
    public IActionResult Upload(string? type)
    {
        var model = new PayrollUploadViewModel
        {
            Year = DateTime.Now.Year,
            Month = DateTime.Now.Month,
            PayrollType = !string.IsNullOrEmpty(type) ? type : "حوافز"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(PayrollUploadViewModel model)
    {
        var filesToProcess = (model.ExcelFiles ?? new List<IFormFile>())
            .Where(f => f != null && f.Length > 0)
            .ToList();

        if (!filesToProcess.Any() && model.ExcelFile != null && model.ExcelFile.Length > 0)
        {
            filesToProcess.Add(model.ExcelFile);
        }

        if (!filesToProcess.Any())
        {
            ModelState.AddModelError("ExcelFiles", "يرجى اختيار ملف إكسيل واحد على الأقل بصيغة .xlsx");
            return View(model);
        }

        foreach (var file in filesToProcess)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                ModelState.AddModelError("ExcelFiles", $"الملف '{file.FileName}' غير مسموح به. الصيغة المسموح بها هي .xlsx فقط.");
                return View(model);
            }
        }

        try
        {
            var fileStreams = filesToProcess.Select(f => (f.OpenReadStream(), f.FileName)).ToList();
            PayrollBatch batch;
            try
            {
                batch = await _parserService.ImportMultiplePayrollAsync(
                    fileStreams,
                    model.Year,
                    model.Month,
                    model.PayrollType,
                    model.AppendToExisting);
            }
            finally
            {
                foreach (var (stream, _) in fileStreams)
                {
                    stream.Dispose();
                }
            }

            string filesText = filesToProcess.Count == 1 ? "ملف واحد" : $"{filesToProcess.Count} ملفات";
            TempData["SuccessMessage"] = $"تم بنجاح استيراد وقراءة {filesText} لـ {model.PayrollType} شهر {model.Month}/{model.Year} بعدد إجمالي {batch.TotalEmployeesCount} موظف وإجمالي صافي: {batch.TotalNetSum:N2} ج.م";

            return RedirectToAction(nameof(Report), new { year = model.Year, month = model.Month, type = model.PayrollType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "حدث خطأ أثناء معالجة ملفات المرتبات/الحوافز.");
            ModelState.AddModelError(string.Empty, "فشلت معالجة الملفات: " + ex.Message);
            return View(model);
        }
    }

    // 1.1 معالجة الرفع التفاعلي مع شريط التقدم الفوري (AJAX Processing)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAjax([FromForm] PayrollUploadViewModel model)
    {
        var filesToProcess = (model.ExcelFiles ?? new List<IFormFile>())
            .Where(f => f != null && f.Length > 0)
            .ToList();

        if (!filesToProcess.Any() && model.ExcelFile != null && model.ExcelFile.Length > 0)
        {
            filesToProcess.Add(model.ExcelFile);
        }

        if (!filesToProcess.Any())
        {
            return Json(new { success = false, message = "يرجى اختيار ملف إكسيل واحد على الأقل بصيغة .xlsx" });
        }

        foreach (var file in filesToProcess)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                return Json(new { success = false, message = $"الملف '{file.FileName}' غير صالح. المسموح به فقط هو ملفات Excel بصيغة .xlsx" });
            }
        }

        try
        {
            var fileStreams = filesToProcess.Select(f => (f.OpenReadStream(), f.FileName)).ToList();
            PayrollBatch batch;
            try
            {
                batch = await _parserService.ImportMultiplePayrollAsync(
                    fileStreams,
                    model.Year,
                    model.Month,
                    model.PayrollType,
                    model.AppendToExisting);
            }
            finally
            {
                foreach (var (stream, _) in fileStreams)
                {
                    stream.Dispose();
                }
            }

            string filesText = filesToProcess.Count == 1 ? "ملف واحد" : $"{filesToProcess.Count} ملفات";
            string successMsg = $"تم بنجاح قراءة وتفريغ {filesText} لـ {model.PayrollType} شهر {model.Month}/{model.Year} بعدد {batch.TotalEmployeesCount} موظف وإجمالي صافي: {batch.TotalNetSum:N2} ج.م";
            TempData["SuccessMessage"] = successMsg;

            return Json(new
            {
                success = true,
                message = successMsg,
                filesCount = filesToProcess.Count,
                count = batch.TotalEmployeesCount,
                totalNet = batch.TotalNetSum.ToString("N2"),
                redirectUrl = Url.Action(nameof(Report), new { year = model.Year, month = model.Month, type = model.PayrollType })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "حدث خطأ أثناء معالجة الملفات عبر AJAX.");
            return Json(new { success = false, message = "فشلت معالجة الملفات: " + ex.Message });
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
