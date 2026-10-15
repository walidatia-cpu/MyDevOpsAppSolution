using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using MyDevOpsApp.Data;
using MyDevOpsApp.Data.Entities;

namespace MyDevOpsApp.Services;

public class PaySlipItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public decimal? Total { get; set; }
}

public class EmployeePaySlipDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string PaymentNumber { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string InsuranceNumber { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public List<PaySlipItemDto> Earnings { get; set; } = new();
    public List<PaySlipItemDto> Deductions { get; set; } = new();

    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetAmount { get; set; }
}

public interface IExcelPayrollParserService
{
    List<EmployeePaySlipDto> ParseExcel(Stream stream);
    Task<PayrollBatch> ImportPayrollAsync(Stream stream, string fileName, int year, int month, string payrollType);
    Task<PayrollBatch> ImportMultiplePayrollAsync(
        IEnumerable<(Stream Stream, string FileName)> files,
        int year,
        int month,
        string payrollType,
        bool appendToExisting = false);
}

public class ExcelPayrollParserService : IExcelPayrollParserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExcelPayrollParserService> _logger;

    public ExcelPayrollParserService(ApplicationDbContext context, ILogger<ExcelPayrollParserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<EmployeePaySlipDto> ParseExcel(Stream stream)
    {
        List<EmployeePaySlipDto> paySlips = new();

        using var package = new ExcelPackage(stream);
        var sheets = package.Workbook.Worksheets.Where(w => w.Dimension != null).ToList();
        if (!sheets.Any())
        {
            _logger.LogWarning("ورقة العمل فارغة أو غير متوفرة في الملف المرفوع.");
            return paySlips;
        }

        foreach (var sheet in sheets)
        {
            int rowCount = sheet.Dimension.Rows;
            EmployeePaySlipDto? currentPaySlip = null;

            for (int row = 1; row <= rowCount; row++)
            {
                var col0 = sheet.Cells[row, 1].Text?.Trim() ?? "";
                var col1 = sheet.Cells[row, 2].Text?.Trim() ?? "";
                var col5 = sheet.Cells[row, 6].Text?.Trim() ?? "";

                // 1. قراءة بيانات الموظف بالكامل
                if (col0.Contains("اسم الموظف:"))
                {
                    if (currentPaySlip != null && (currentPaySlip.Earnings.Any() || currentPaySlip.NetAmount > 0))
                    {
                        FinalizePaySlipTotals(currentPaySlip);
                        paySlips.Add(currentPaySlip);
                    }

                    currentPaySlip = new();

                    var match1 = Regex.Match(col0, """اسم الموظف:\s*(.*?)\s*رقم الصرف:\s*(.*?)\s*الرقم القوم[يى]:\s*(.*)""");
                    if (match1.Success)
                    {
                        currentPaySlip.EmployeeName = match1.Groups[1].Value.Trim();
                        currentPaySlip.PaymentNumber = match1.Groups[2].Value.Trim();
                        currentPaySlip.NationalId = match1.Groups[3].Value.Trim();
                    }
                    else
                    {
                        var nameMatch = Regex.Match(col0, @"اسم الموظف:\s*([^\r\n]*)");
                        if (nameMatch.Success)
                        {
                            currentPaySlip.EmployeeName = nameMatch.Groups[1].Value.Trim();
                        }
                    }

                    var row2Text = sheet.Cells[row + 1, 1].Text?.Trim() ?? "";
                    var match2 = Regex.Match(row2Text, """نوع الصرف:\s*(.*?)\s*البنك:\s*(.*?)\s*الفرع:\s*(.*)""");
                    if (match2.Success)
                    {
                        currentPaySlip.PaymentType = match2.Groups[1].Value.Trim();
                        currentPaySlip.BankName = match2.Groups[2].Value.Trim();
                        currentPaySlip.Branch = match2.Groups[3].Value.Trim();
                    }

                    var row3Text = sheet.Cells[row + 2, 1].Text?.Trim() ?? "";
                    var match3 = Regex.Match(row3Text, """الرقم التامين[يى]:\s*(.*?)\s*الدرجة:\s*(.*?)\s*الادارة:\s*(.*)""");
                    if (match3.Success)
                    {
                        currentPaySlip.InsuranceNumber = match3.Groups[1].Value.Trim();
                        currentPaySlip.Grade = match3.Groups[2].Value.Trim();
                        currentPaySlip.Department = match3.Groups[3].Value.Trim();
                    }

                    row += 2;
                    continue;
                }

                if (currentPaySlip is null) continue;

                // 2. قراءة تفاصيل الاستحقاقات مع استثناء العناوين
                if (!string.IsNullOrEmpty(col1) && col1 != "الاسم" && col1 != "أجمالى الاستحقاقات" && col1 != "الاجر المكمل" && !col1.Contains("حكومة") && !col1.Contains("فرع (1)"))
                {
                    currentPaySlip.Earnings.Add(new()
                    {
                        Code = sheet.Cells[row, 1].Text?.Trim() ?? "",
                        Name = col1,
                        Value = ParseDecimal(sheet.Cells[row, 3].Text),
                        Total = ParseDecimal(sheet.Cells[row, 4].Text)
                    });
                }

                // 3. قراءة تفاصيل الاستقطاعات
                if (!string.IsNullOrEmpty(col5) && col5 != "الاسم" && col5 != "الأجمالى الاستقطاعات")
                {
                    currentPaySlip.Deductions.Add(new()
                    {
                        Code = sheet.Cells[row, 5].Text?.Trim() ?? "",
                        Name = col5,
                        Value = ParseDecimal(sheet.Cells[row, 7].Text),
                        Total = ParseDecimal(sheet.Cells[row, 8].Text)
                    });
                }

                // 4. قراءة الإجماليات
                if (col1 == "أجمالى الاستحقاقات")
                {
                    currentPaySlip.TotalEarnings = ParseDecimal(sheet.Cells[row, 4].Text) ?? 0;
                }

                if (col5 == "الأجمالى الاستقطاعات")
                {
                    currentPaySlip.TotalDeductions = ParseDecimal(sheet.Cells[row, 8].Text) ?? 0;
                }

                // 5. نهاية الموظف (الصافي)
                if (col0.Contains("الصافى:") || col0.Contains("الصافي:"))
                {
                    currentPaySlip.NetAmount = ParseDecimal(sheet.Cells[row, 5].Text) ?? 0;
                    FinalizePaySlipTotals(currentPaySlip);
                    paySlips.Add(currentPaySlip);
                    currentPaySlip = null;
                }
            }

            if (currentPaySlip != null && (currentPaySlip.Earnings.Any() || currentPaySlip.NetAmount > 0))
            {
                FinalizePaySlipTotals(currentPaySlip);
                paySlips.Add(currentPaySlip);
            }
        }

        return paySlips;
    }

    private static void FinalizePaySlipTotals(EmployeePaySlipDto slip)
    {
        if (slip.TotalEarnings == 0 && slip.Earnings.Any())
        {
            slip.TotalEarnings = slip.Earnings.Sum(e => e.Total ?? e.Value ?? 0);
        }
        if (slip.TotalDeductions == 0 && slip.Deductions.Any())
        {
            slip.TotalDeductions = slip.Deductions.Sum(d => d.Total ?? d.Value ?? 0);
        }
        if (slip.NetAmount == 0 && (slip.TotalEarnings > 0 || slip.TotalDeductions > 0))
        {
            slip.NetAmount = slip.TotalEarnings - slip.TotalDeductions;
        }
    }

    public Task<PayrollBatch> ImportPayrollAsync(Stream stream, string fileName, int year, int month, string payrollType)
    {
        return ImportMultiplePayrollAsync(new[] { (stream, fileName) }, year, month, payrollType, appendToExisting: false);
    }

    public async Task<PayrollBatch> ImportMultiplePayrollAsync(
        IEnumerable<(Stream Stream, string FileName)> files,
        int year,
        int month,
        string payrollType,
        bool appendToExisting = false)
    {
        var fileList = files.ToList();
        if (!fileList.Any())
        {
            throw new ArgumentException("لم يتم تحديد أي ملفات للمعالجة.", nameof(files));
        }

        var allRawSlips = new List<EmployeePaySlipDto>();
        var processedFileNames = new List<string>();

        foreach (var (stream, fileName) in fileList)
        {
            var parsed = ParseExcel(stream);
            if (parsed.Any())
            {
                allRawSlips.AddRange(parsed);
                processedFileNames.Add(Path.GetFileName(fileName));
                _logger.LogInformation("تمت قراءة {Count} موظف من الملف: {FileName}", parsed.Count, fileName);
            }
            else
            {
                _logger.LogWarning("الملف {FileName} لم يحتوِ على أي كشوف صالحة أو مطابق للشروط.", fileName);
            }
        }

        if (allRawSlips.Count == 0)
        {
            throw new InvalidOperationException("لم يتم العثور على أي بيانات موظفين صحيحة في الملفات المحددة. يرجى التأكد من اختيار ملفات Excel بصيغة .xlsx ومطابقة لنموذج كشف المرتبات أو الحوافز.");
        }

        // =========================================================================
        // تجميع ودمج سجلات الموظفين عبر كافة الملفات المرفوعة لنفس الشهر
        // =========================================================================
        var mergedSlipsByKey = new Dictionary<string, EmployeePaySlipDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var slip in allRawSlips)
        {
            string key = !string.IsNullOrWhiteSpace(slip.NationalId)
                ? "NID_" + slip.NationalId.Trim()
                : (!string.IsNullOrWhiteSpace(slip.PaymentNumber)
                    ? "PAY_" + slip.PaymentNumber.Trim()
                    : (!string.IsNullOrWhiteSpace(slip.EmployeeName) ? "NAME_" + slip.EmployeeName.Trim() : ""));

            if (string.IsNullOrEmpty(key)) continue;

            if (!mergedSlipsByKey.TryGetValue(key, out var existing))
            {
                mergedSlipsByKey[key] = new EmployeePaySlipDto
                {
                    EmployeeName = slip.EmployeeName,
                    PaymentNumber = slip.PaymentNumber,
                    NationalId = slip.NationalId,
                    PaymentType = slip.PaymentType,
                    BankName = slip.BankName,
                    Branch = slip.Branch,
                    InsuranceNumber = slip.InsuranceNumber,
                    Grade = slip.Grade,
                    Department = slip.Department,
                    TotalEarnings = slip.TotalEarnings,
                    TotalDeductions = slip.TotalDeductions,
                    NetAmount = slip.NetAmount,
                    Earnings = new List<PaySlipItemDto>(slip.Earnings),
                    Deductions = new List<PaySlipItemDto>(slip.Deductions)
                };
            }
            else
            {
                // دمج بيانات الموظف من الملفات المتعددة لشهر الصرف
                if (string.IsNullOrEmpty(existing.Department) && !string.IsNullOrEmpty(slip.Department)) existing.Department = slip.Department;
                if (string.IsNullOrEmpty(existing.Grade) && !string.IsNullOrEmpty(slip.Grade)) existing.Grade = slip.Grade;
                if (string.IsNullOrEmpty(existing.BankName) && !string.IsNullOrEmpty(slip.BankName)) existing.BankName = slip.BankName;
                if (string.IsNullOrEmpty(existing.Branch) && !string.IsNullOrEmpty(slip.Branch)) existing.Branch = slip.Branch;
                if (string.IsNullOrEmpty(existing.InsuranceNumber) && !string.IsNullOrEmpty(slip.InsuranceNumber)) existing.InsuranceNumber = slip.InsuranceNumber;
                if (string.IsNullOrEmpty(existing.PaymentType) && !string.IsNullOrEmpty(slip.PaymentType)) existing.PaymentType = slip.PaymentType;
                if (string.IsNullOrEmpty(existing.EmployeeName) && !string.IsNullOrEmpty(slip.EmployeeName)) existing.EmployeeName = slip.EmployeeName;

                // إضافة بنود الاستحقاقات والاستقطاعات
                existing.Earnings.AddRange(slip.Earnings);
                existing.Deductions.AddRange(slip.Deductions);

                // دمج المبالغ
                existing.TotalEarnings += slip.TotalEarnings;
                existing.TotalDeductions += slip.TotalDeductions;
                existing.NetAmount += slip.NetAmount;

                if (existing.NetAmount == 0 && (existing.TotalEarnings > 0 || existing.TotalDeductions > 0))
                {
                    existing.NetAmount = existing.TotalEarnings - existing.TotalDeductions;
                }
            }
        }

        var uniqueSlips = mergedSlipsByKey.Values.ToList();

        // =========================================================================
        // معالجة الدفعة في قاعدة البيانات (استبدال أو إضافة)
        // =========================================================================
        PayrollBatch batch;
        if (appendToExisting)
        {
            var existingBatch = await _context.PayrollBatches
                .Include(b => b.Records)
                .ThenInclude(r => r.Items)
                .FirstOrDefaultAsync(b => b.Year == year && b.Month == month && b.PayrollType == payrollType);

            if (existingBatch != null)
            {
                batch = existingBatch;
                var currentFiles = batch.OriginalFileName?
                    .Split(new[] { " | ", " ، ", ", " }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList() ?? new List<string>();

                foreach (var name in processedFileNames)
                {
                    if (!currentFiles.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        currentFiles.Add(name);
                    }
                }

                batch.OriginalFileName = BuildBatchSummaryFileName(currentFiles);
                batch.UploadedAt = DateTime.Now;
            }
            else
            {
                batch = new PayrollBatch
                {
                    Year = year,
                    Month = month,
                    PayrollType = payrollType,
                    OriginalFileName = BuildBatchSummaryFileName(processedFileNames),
                    UploadedAt = DateTime.Now
                };
                _context.PayrollBatches.Add(batch);
            }
        }
        else
        {
            var existingBatches = await _context.PayrollBatches
                .Include(b => b.Records)
                .ThenInclude(r => r.Items)
                .Where(b => b.Year == year && b.Month == month && b.PayrollType == payrollType)
                .ToListAsync();

            if (existingBatches.Any())
            {
                _context.PayrollBatches.RemoveRange(existingBatches);
                await _context.SaveChangesAsync();
            }

            batch = new PayrollBatch
            {
                Year = year,
                Month = month,
                PayrollType = payrollType,
                OriginalFileName = BuildBatchSummaryFileName(processedFileNames),
                UploadedAt = DateTime.Now
            };
            _context.PayrollBatches.Add(batch);
        }

        // =========================================================================
        // تحسين الأداء: جلب الموظفين في قاموس O(1)
        // =========================================================================
        var existingEmployees = await _context.Employees.ToListAsync();
        var byNationalId = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);
        var byPaymentNo = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);

        foreach (var emp in existingEmployees)
        {
            if (!string.IsNullOrWhiteSpace(emp.NationalId) && !byNationalId.ContainsKey(emp.NationalId.Trim()))
            {
                byNationalId[emp.NationalId.Trim()] = emp;
            }
            if (!string.IsNullOrWhiteSpace(emp.PaymentNumber) && !byPaymentNo.ContainsKey(emp.PaymentNumber.Trim()))
            {
                byPaymentNo[emp.PaymentNumber.Trim()] = emp;
            }
        }

        // قاموس لسجلات الدفعة الحالية إذا كانت موجودة مسبقاً (في حالة الدمج)
        var existingRecordsByEmpId = batch.Records.ToDictionary(r => r.EmployeeId);

        var newRecords = new List<PayrollRecord>();
        var newItems = new List<PayrollItem>();

        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            foreach (var slip in uniqueSlips)
            {
                var nationalId = slip.NationalId.Trim();
                var paymentNumber = slip.PaymentNumber.Trim();

                Employee? employee = null;
                if (!string.IsNullOrEmpty(nationalId) && byNationalId.TryGetValue(nationalId, out var emp1))
                {
                    employee = emp1;
                }
                else if (!string.IsNullOrEmpty(paymentNumber) && byPaymentNo.TryGetValue(paymentNumber, out var emp2))
                {
                    employee = emp2;
                }

                if (employee == null)
                {
                    employee = new Employee
                    {
                        Name = slip.EmployeeName,
                        NationalId = nationalId,
                        PaymentNumber = paymentNumber,
                        InsuranceNumber = slip.InsuranceNumber,
                        Grade = slip.Grade,
                        Department = slip.Department,
                        BankName = slip.BankName,
                        Branch = slip.Branch,
                        CreatedAt = DateTime.Now
                    };
                    _context.Employees.Add(employee);

                    if (!string.IsNullOrEmpty(nationalId)) byNationalId[nationalId] = employee;
                    if (!string.IsNullOrEmpty(paymentNumber)) byPaymentNo[paymentNumber] = employee;
                }
                else
                {
                    if (!string.IsNullOrEmpty(slip.EmployeeName)) employee.Name = slip.EmployeeName;
                    if (!string.IsNullOrEmpty(slip.InsuranceNumber)) employee.InsuranceNumber = slip.InsuranceNumber;
                    if (!string.IsNullOrEmpty(slip.Grade)) employee.Grade = slip.Grade;
                    if (!string.IsNullOrEmpty(slip.Department)) employee.Department = slip.Department;
                    if (!string.IsNullOrEmpty(slip.BankName)) employee.BankName = slip.BankName;
                    if (!string.IsNullOrEmpty(slip.Branch)) employee.Branch = slip.Branch;
                }

                // فحص ما إذا كان هناك سجل سابق لنفس الموظف في نفس الدفعة (في حالة appendToExisting)
                if (employee.Id > 0 && existingRecordsByEmpId.TryGetValue(employee.Id, out var existingRecord))
                {
                    existingRecord.TotalEarnings += slip.TotalEarnings;
                    existingRecord.TotalDeductions += slip.TotalDeductions;
                    existingRecord.NetAmount += slip.NetAmount;

                    foreach (var earn in slip.Earnings)
                    {
                        var item = new PayrollItem
                        {
                            PayrollRecord = existingRecord,
                            ItemType = "Earning",
                            Code = earn.Code,
                            Name = earn.Name,
                            Value = earn.Value,
                            Total = earn.Total
                        };
                        existingRecord.Items.Add(item);
                        newItems.Add(item);
                    }

                    foreach (var ded in slip.Deductions)
                    {
                        var item = new PayrollItem
                        {
                            PayrollRecord = existingRecord,
                            ItemType = "Deduction",
                            Code = ded.Code,
                            Name = ded.Name,
                            Value = ded.Value,
                            Total = ded.Total
                        };
                        existingRecord.Items.Add(item);
                        newItems.Add(item);
                    }
                }
                else
                {
                    var record = new PayrollRecord
                    {
                        PayrollBatch = batch,
                        Employee = employee,
                        EmployeeName = slip.EmployeeName,
                        PaymentNumber = slip.PaymentNumber,
                        NationalId = slip.NationalId,
                        PaymentType = slip.PaymentType,
                        BankName = slip.BankName,
                        Branch = slip.Branch,
                        InsuranceNumber = slip.InsuranceNumber,
                        Grade = slip.Grade,
                        Department = slip.Department,
                        TotalEarnings = slip.TotalEarnings,
                        TotalDeductions = slip.TotalDeductions,
                        NetAmount = slip.NetAmount
                    };

                    foreach (var earn in slip.Earnings)
                    {
                        var item = new PayrollItem
                        {
                            PayrollRecord = record,
                            ItemType = "Earning",
                            Code = earn.Code,
                            Name = earn.Name,
                            Value = earn.Value,
                            Total = earn.Total
                        };
                        record.Items.Add(item);
                        newItems.Add(item);
                    }

                    foreach (var ded in slip.Deductions)
                    {
                        var item = new PayrollItem
                        {
                            PayrollRecord = record,
                            ItemType = "Deduction",
                            Code = ded.Code,
                            Name = ded.Name,
                            Value = ded.Value,
                            Total = ded.Total
                        };
                        record.Items.Add(item);
                        newItems.Add(item);
                    }

                    batch.Records.Add(record);
                    newRecords.Add(record);
                }
            }

            // إدراج السجلات والبنود في DbContext صراحة لتتبعها وحفظها في قاعدة البيانات
            if (newRecords.Any())
            {
                await _context.PayrollRecords.AddRangeAsync(newRecords);
            }
            if (newItems.Any())
            {
                await _context.PayrollItems.AddRangeAsync(newItems);
            }

            // تحديث إجماليات الدفعة بدقة
            batch.TotalEmployeesCount = batch.Records.Count;
            batch.TotalEarningsSum = batch.Records.Sum(r => r.TotalEarnings);
            batch.TotalDeductionsSum = batch.Records.Sum(r => r.TotalDeductions);
            batch.TotalNetSum = batch.Records.Sum(r => r.NetAmount);

            // تشغيل فحص التغييرات الصريح وتثبيت كافة السجلات في قاعدة البيانات
            _context.ChangeTracker.DetectChanges();
            await _context.SaveChangesAsync();
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return batch;
    }

    private static string BuildBatchSummaryFileName(List<string> fileNames)
    {
        if (!fileNames.Any()) return "ملف_إكسيل.xlsx";
        if (fileNames.Count == 1) return fileNames[0];

        string summary = $"({fileNames.Count} ملفات): " + string.Join(" | ", fileNames);
        if (summary.Length > 480)
        {
            summary = summary.Substring(0, 475) + "...";
        }
        return summary;
    }

    private static decimal? ParseDecimal(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        if (decimal.TryParse(val.Replace(",", "").Trim(), out decimal result)) return result;
        return null;
    }
}
