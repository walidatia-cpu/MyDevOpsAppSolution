using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Data.Entities;
using MyDevOpsApp.Models;

namespace MyDevOpsApp.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>();

        // التأكد من وجود قاعدة البيانات والجداول كاملة (مع معالجة أي جداول جديدة تمت إضافتها)
        bool needRecreate = false;
        try
        {
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM PayrollBatches LIMIT 1;");
            await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM Employees LIMIT 1;");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "الجداول غير مكتملة أو قاعدة البيانات بحاجة إلى إعادة بناء.");
            needRecreate = true;
        }

        if (needRecreate)
        {
            try
            {
                await context.Database.EnsureDeletedAsync();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "تعذر حذف قاعدة البيانات القديمة، سيتم المتابعة لإنشاء الجداول.");
            }
            await context.Database.EnsureCreatedAsync();
        }

        // التأكد من توفر حقلي CreatedAt و LastLoginAt في جدول AspNetUsers
        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE AspNetUsers ADD COLUMN CreatedAt TEXT NULL;");
        }
        catch { /* العمود موجود مسبقاً */ }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE AspNetUsers ADD COLUMN LastLoginAt TEXT NULL;");
        }
        catch { /* العمود موجود مسبقاً */ }

        try
        {
            await context.Database.ExecuteSqlRawAsync("UPDATE AspNetUsers SET CreatedAt = datetime('now') WHERE CreatedAt IS NULL;");
        }
        catch { }

        // 1. إنشاء الأدوار (Roles)
        string[] roles = { "Admin", "Employee" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. إنشاء المستخدم الافتراضي للأدمن (Default Admin User)
        var defaultAdminEmail = "admin@mydevopsapp.com";
        var defaultAdmin = await userManager.FindByEmailAsync(defaultAdminEmail);
        if (defaultAdmin == null)
        {
            defaultAdmin = new ApplicationUser
            {
                UserName = defaultAdminEmail,
                Email = defaultAdminEmail,
                FullName = "مدير النظام (System Admin)",
                Department = "إدارة التكنولوجيا والبنية التحتية",
                CreatedAt = DateTime.Now.AddDays(-30),
                LastLoginAt = DateTime.Now,
                EmailConfirmed = true
            };

            var createAdminResult = await userManager.CreateAsync(defaultAdmin, "Admin@123456");
            if (createAdminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultAdmin, "Admin");
            }
        }

        // 3. إنشاء موظف نموذجي في جدول الموظفين (Employees) لتمكين الربط والاستعلام الفوري
        var sampleNationalId = "29501011234567";
        var samplePaymentNumber = "1001";
        var sampleEmployee = await context.Employees.FirstOrDefaultAsync(e => e.NationalId == sampleNationalId);
        if (sampleEmployee == null)
        {
            sampleEmployee = new Employee
            {
                NationalId = sampleNationalId,
                PaymentNumber = samplePaymentNumber,
                Name = "أحمد محمود سالم",
                Department = "هندسة البرمجيات",
                Grade = "الأولى التخصصية",
                CreatedAt = DateTime.UtcNow
            };
            context.Employees.Add(sampleEmployee);
            await context.SaveChangesAsync();
        }

        // 4. إنشاء مستخدم افتراضي للموظف (Default Employee User)
        var defaultEmployeeEmail = "employee@mydevopsapp.com";
        var defaultEmployee = await userManager.FindByEmailAsync(defaultEmployeeEmail);
        if (defaultEmployee == null)
        {
            defaultEmployee = new ApplicationUser
            {
                UserName = defaultEmployeeEmail,
                Email = defaultEmployeeEmail,
                FullName = sampleEmployee.Name,
                Department = sampleEmployee.Department ?? "هندسة البرمجيات",
                NationalId = sampleNationalId,
                PaymentNumber = samplePaymentNumber,
                EmployeeId = sampleEmployee.Id,
                CreatedAt = DateTime.Now.AddDays(-15),
                LastLoginAt = DateTime.Now.AddHours(-2),
                EmailConfirmed = true
            };

            var createEmpResult = await userManager.CreateAsync(defaultEmployee, "Employee@123456");
            if (createEmpResult.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultEmployee, "Employee");
                sampleEmployee.UserId = defaultEmployee.Id;
                await context.SaveChangesAsync();
            }
        }
        else if (string.IsNullOrEmpty(defaultEmployee.NationalId) || defaultEmployee.EmployeeId == null)
        {
            defaultEmployee.NationalId = sampleNationalId;
            defaultEmployee.PaymentNumber = samplePaymentNumber;
            defaultEmployee.EmployeeId = sampleEmployee.Id;
            await userManager.UpdateAsync(defaultEmployee);
            sampleEmployee.UserId = defaultEmployee.Id;
            await context.SaveChangesAsync();
        }

        // 5. إنشاء مسير رواتب تجريبي إن لم توجد بيانات مسبقة
        if (!await context.PayrollBatches.AnyAsync())
        {
            var now = DateTime.Now;
            var sampleBatch = new PayrollBatch
            {
                Year = now.Year,
                Month = now.Month,
                PayrollType = "مرتبات",
                OriginalFileName = "Initial_Payroll.xlsx",
                TotalEmployeesCount = 1,
                TotalEarningsSum = 15000,
                TotalDeductionsSum = 2150,
                TotalNetSum = 12850,
                UploadedAt = DateTime.UtcNow
            };
            context.PayrollBatches.Add(sampleBatch);
            await context.SaveChangesAsync();

            var sampleRecord = new PayrollRecord
            {
                PayrollBatchId = sampleBatch.Id,
                EmployeeId = sampleEmployee.Id,
                EmployeeName = sampleEmployee.Name,
                PaymentNumber = sampleEmployee.PaymentNumber,
                NationalId = sampleEmployee.NationalId,
                Department = sampleEmployee.Department ?? "هندسة البرمجيات",
                TotalEarnings = 15000,
                TotalDeductions = 2150,
                NetAmount = 12850
            };
            context.PayrollRecords.Add(sampleRecord);
            await context.SaveChangesAsync();

            var sampleItems = new List<PayrollItem>
            {
                new PayrollItem { PayrollRecordId = sampleRecord.Id, ItemType = "Earning", Code = "101", Name = "الراتب الأساسي", Total = 12500 },
                new PayrollItem { PayrollRecordId = sampleRecord.Id, ItemType = "Earning", Code = "102", Name = "بدل طبيعة عمل", Total = 1500 },
                new PayrollItem { PayrollRecordId = sampleRecord.Id, ItemType = "Earning", Code = "103", Name = "حافز تميز إضافي", Total = 1000 },
                new PayrollItem { PayrollRecordId = sampleRecord.Id, ItemType = "Deduction", Code = "201", Name = "التأمينات الاجتماعية", Total = 1400 },
                new PayrollItem { PayrollRecordId = sampleRecord.Id, ItemType = "Deduction", Code = "202", Name = "ضريبة كسب العمل", Total = 750 }
            };
            context.PayrollItems.AddRange(sampleItems);
            await context.SaveChangesAsync();
        }
    }
}
