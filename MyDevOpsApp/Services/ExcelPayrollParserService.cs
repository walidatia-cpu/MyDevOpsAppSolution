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
        var sheet = package.Workbook.Worksheets.FirstOrDefault();
        if (sheet == null || sheet.Dimension == null)
        {
            _logger.LogWarning("ورقة العمل فارغة أو غير متوفرة في الملف المرفوع.");
            return paySlips;
        }

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
                currentPaySlip = new();

                var match1 = Regex.Match(col0, """اسم الموظف:\s*(.*?)\s*رقم الصرف:\s*(.*?)الرقم القومى:\s*(.*)""");
                if (match1.Success)
                {
                    currentPaySlip.EmployeeName = match1.Groups[1].Value.Trim();
                    currentPaySlip.PaymentNumber = match1.Groups[2].Value.Trim();
                    currentPaySlip.NationalId = match1.Groups[3].Value.Trim();
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
                var match3 = Regex.Match(row3Text, """الرقم التامينى:\s*(.*?)\s*الدرجة:\s*(.*?)\s*الادارة:\s*(.*)""");
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
            if (col0.Contains("الصافى:"))
            {
                currentPaySlip.NetAmount = ParseDecimal(sheet.Cells[row, 5].Text) ?? 0;
                paySlips.Add(currentPaySlip);
                currentPaySlip = null;
            }
        }

        return paySlips;
    }

    public async Task<PayrollBatch> ImportPayrollAsync(Stream stream, string fileName, int year, int month, string payrollType)
    {
        var rawSlips = ParseExcel(stream);
        if (rawSlips.Count == 0)
        {
            throw new InvalidOperationException("لم يتم العثور على أي بيانات موظفين صحيحة في الملف.");
        }

        // =========================================================================
        // قاعدة منع التكرار 1: منع تكرار نفس الموظف داخل نفس ملف الصرف للشهر المحدد
        // =========================================================================
        var uniqueSlips = new List<EmployeePaySlipDto>();
        var seenEmployeeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var slip in rawSlips)
        {
            // نعتمد على الرقم القومي أولاً، أو رقم الملف ثانياً لتعريف الموظف بدقة
            string key = !string.IsNullOrWhiteSpace(slip.NationalId)
                ? "NID_" + slip.NationalId.Trim()
                : (!string.IsNullOrWhiteSpace(slip.PaymentNumber) ? "PAY_" + slip.PaymentNumber.Trim() : "");

            if (string.IsNullOrEmpty(key)) continue;

            if (seenEmployeeKeys.Contains(key))
            {
                _logger.LogWarning("تم تجاوز سجل مكرر للموظف {Name} بالرقم {Key} لمنع وجود أكثر من مرتب/حافز في نفس الشهر.", slip.EmployeeName, key);
                continue; // تخطي التكرار داخل نفس الملف
            }

            seenEmployeeKeys.Add(key);
            uniqueSlips.Add(slip);
        }

        // =========================================================================
        // قاعدة منع التكرار 2: إذا كان هناك دفعة مسجلة مسبقاً لنفس الشهر والنوع،
        // نقوم بمسحها تماماً واستبدالها بالدفعة الجديدة (لمنع تكرار المرتبات لنفس الشهر)
        // =========================================================================
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

        var batch = new PayrollBatch
        {
            Year = year,
            Month = month,
            PayrollType = payrollType,
            OriginalFileName = fileName,
            UploadedAt = DateTime.Now,
            TotalEmployeesCount = uniqueSlips.Count,
            TotalEarningsSum = uniqueSlips.Sum(s => s.TotalEarnings),
            TotalDeductionsSum = uniqueSlips.Sum(s => s.TotalDeductions),
            TotalNetSum = uniqueSlips.Sum(s => s.NetAmount)
        };

        // =========================================================================
        // تحسين الأداء 50x: جلب الموظفين في قاموس سريع O(1) بدلاً من البحث الخطي المتكرر
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

        // إيقاف ChangeTracker مؤقتاً لتسريع إدخال آلاف السجلات دفعة واحدة
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var addedEmployeeIdsInBatch = new HashSet<int>();

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
                    // تحديث بيانات الموظف إن طرأ أي تغيير
                    if (!string.IsNullOrEmpty(slip.EmployeeName)) employee.Name = slip.EmployeeName;
                    if (!string.IsNullOrEmpty(slip.InsuranceNumber)) employee.InsuranceNumber = slip.InsuranceNumber;
                    if (!string.IsNullOrEmpty(slip.Grade)) employee.Grade = slip.Grade;
                    if (!string.IsNullOrEmpty(slip.Department)) employee.Department = slip.Department;
                    if (!string.IsNullOrEmpty(slip.BankName)) employee.BankName = slip.BankName;
                    if (!string.IsNullOrEmpty(slip.Branch)) employee.Branch = slip.Branch;
                }

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
                    record.Items.Add(new PayrollItem
                    {
                        ItemType = "Earning",
                        Code = earn.Code,
                        Name = earn.Name,
                        Value = earn.Value,
                        Total = earn.Total
                    });
                }

                foreach (var ded in slip.Deductions)
                {
                    record.Items.Add(new PayrollItem
                    {
                        ItemType = "Deduction",
                        Code = ded.Code,
                        Name = ded.Name,
                        Value = ded.Value,
                        Total = ded.Total
                    });
                }

                batch.Records.Add(record);
            }

            _context.PayrollBatches.Add(batch);
            await _context.SaveChangesAsync();
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return batch;
    }

    private static decimal? ParseDecimal(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        if (decimal.TryParse(val.Replace(",", "").Trim(), out decimal result)) return result;
        return null;
    }
}
