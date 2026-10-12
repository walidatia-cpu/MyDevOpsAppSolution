using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Models;

namespace MyDevOpsApp.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // التأكد من وجود قاعدة البيانات والجداول كاملة (مع معالجة أي جداول جديدة تمت إضافتها)
        bool needRecreate = false;
        try
        {
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM PayrollBatches LIMIT 1;");
            await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM Employees LIMIT 1;");
        }
        catch
        {
            needRecreate = true;
        }

        if (needRecreate)
        {
            // إعادة بناء قاعدة البيانات بالجداول الجديدة كاملة
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

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
                EmailConfirmed = true
            };

            var createAdminResult = await userManager.CreateAsync(defaultAdmin, "Admin@123456");
            if (createAdminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultAdmin, "Admin");
            }
        }

        // 3. إنشاء مستخدم افتراضي للموظف (Default Employee User)
        var defaultEmployeeEmail = "employee@mydevopsapp.com";
        var defaultEmployee = await userManager.FindByEmailAsync(defaultEmployeeEmail);
        if (defaultEmployee == null)
        {
            defaultEmployee = new ApplicationUser
            {
                UserName = defaultEmployeeEmail,
                Email = defaultEmployeeEmail,
                FullName = "أحمد محمود سالم",
                Department = "هندسة البرمجيات",
                EmailConfirmed = true
            };

            var createEmpResult = await userManager.CreateAsync(defaultEmployee, "Employee@123456");
            if (createEmpResult.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultEmployee, "Employee");
            }
        }
    }
}
