using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDevOpsApp.Areas.Employees.Models;

namespace MyDevOpsApp.Areas.Employees.Controllers;

[Area("Employees")]
[Authorize(Roles = "Admin,Employee")]
public class TasksController : Controller
{
    private static readonly List<TaskItemModel> MyTasksList = new()
    {
        new() { Id = 101, Title = "تحديث خط أنابيب النشر الآلي (CI/CD Pipeline)", Description = "إعداد مراحل البناء والاختبار والنشر الآلي على بيئة الاختبار والإنتاج", Priority = "عالية", Status = "قيد التنفيذ", DueTime = "اليوم - 02:00 م" },
        new() { Id = 102, Title = "مراجعة إعدادات الأمان في كتل Kubernetes", Description = "فحص امتيازات الحاويات وقواعد الشبكة ومفاتيح المصادقة", Priority = "متوسطة", Status = "قيد الانتظار", DueTime = "اليوم - 04:30 م" },
        new() { Id = 103, Title = "توثيق بنية مشروع MVC الجديد والـ Areas", Description = "كتابة دليل المطور لتنظيم ملفات Controllers و Views و Routing", Priority = "عادية", Status = "مكتملة", DueTime = "أمس" },
        new() { Id = 104, Title = "فحص اختبارات الوحدة (Unit Tests)", Description = "التحقق من تغطية كود الاختبارات لمسارات الـ Controller المختلفة", Priority = "عالية", Status = "مكتملة", DueTime = "أمس" },
        new() { Id = 105, Title = "تحسين أداء تحميل صفحات الإدارة", Description = "تقليل حجم الحزم وضغط الأصول الثابتة لتسريع الاستجابة", Priority = "متوسطة", Status = "قيد الانتظار", DueTime = "غداً - 12:00 م" }
    };

    public IActionResult Index()
    {
        return View(MyTasksList);
    }
}
