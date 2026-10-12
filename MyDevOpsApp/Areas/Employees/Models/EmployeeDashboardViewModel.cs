namespace MyDevOpsApp.Areas.Employees.Models;

public class EmployeeDashboardViewModel
{
    public string EmployeeName { get; set; } = "أحمد محمود سالم";
    public string JobTitle { get; set; } = "مهندس DevOps أول";
    public string Department { get; set; } = "هندسة البرمجيات والبنية التحتية";
    public int CompletedTasksThisMonth { get; set; } = 28;
    public int PendingTasks { get; set; } = 5;
    public int AnnualLeaveBalance { get; set; } = 16;
    public string AttendanceStatus { get; set; } = "تم تسجيل الحضور 08:45 ص";
    
    public List<TaskItemModel> TodayTasks { get; set; } = new()
    {
        new() { Id = 101, Title = "تحديث خط أنابيب النشر الآلي (CI/CD Pipeline)", Priority = "عالية", Status = "قيد التنفيذ", DueTime = "02:00 م" },
        new() { Id = 102, Title = "مراجعة إعدادات الأمان في كتل Kubernetes", Priority = "متوسطة", Status = "قيد الانتظار", DueTime = "04:30 م" },
        new() { Id = 103, Title = "توثيق بنية مشروع MVC الجديد والـ Areas", Priority = "عادية", Status = "مكتملة", DueTime = "11:00 ص" }
    };

    public List<LeaveRequestModel> RecentRequests { get; set; } = new()
    {
        new() { Id = 1, RequestType = "إجازة اعتيادية", StartDate = "2026-11-01", EndDate = "2026-11-05", Status = "موافق عليها", BadgeClass = "badge-success" },
        new() { Id = 2, RequestType = "إذن انصراف مبكر", StartDate = "2026-10-18", EndDate = "2026-10-18", Status = "قيد المراجعة", BadgeClass = "badge-warning" }
    };
}
