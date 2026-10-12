using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Data;
using MyDevOpsApp.Models;
using MyDevOpsApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. إعداد قاعدة البيانات SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. إعداد ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // قواعد مرنة لكلمة المرور لتسهيل التجربة
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. إعداد مسارات ملف تعريف الارتباط (Cookie Settings)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// 4. خدمات قراءة ومعالجة ملفات المرتبات Excel
builder.Services.AddScoped<IExcelPayrollParserService, ExcelPayrollParserService>();

// 5. إضافة خدمات MVC مع الـ Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. تهيئة قاعدة البيانات وإنشاء المستخدمين الافتراضيين (Admin & Employee)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "حدث خطأ أثناء تهيئة قاعدة البيانات ومستخدمي النظام.");
    }
}

// 6. مسار معالجة الأخطاء والأمان
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 7. تفعيل المصادقة والتفويض (Authentication & Authorization)
app.UseAuthentication();
app.UseAuthorization();

// 8. مسارات الـ Areas
app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "Admin/{controller=Payroll}/{action=Report}/{id?}");

app.MapAreaControllerRoute(
    name: "EmployeesArea",
    areaName: "Employees",
    pattern: "Employees/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 9. المسار الافتراضي للبوابة
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
