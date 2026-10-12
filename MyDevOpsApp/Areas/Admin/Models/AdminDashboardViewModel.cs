namespace MyDevOpsApp.Areas.Admin.Models;

public class AdminDashboardViewModel
{
    public int TotalEmployees { get; set; } = 148;
    public int ActiveProjects { get; set; } = 12;
    public int PendingRequests { get; set; } = 7;
    public double SystemUptimePercent { get; set; } = 99.98;
    public List<RecentActivityItem> RecentActivities { get; set; } = new()
    {
        new() { Action = "إضافة موظف جديد", User = "أحمد محمود", Timestamp = "منذ 15 دقيقة", BadgeClass = "badge-success" },
        new() { Action = "تحديث صلاحيات النظام", User = "سارة إبراهيم", Timestamp = "منذ ساعة واحدة", BadgeClass = "badge-primary" },
        new() { Action = "طلب إجازة معلق للمراجعة", User = "محمد علي", Timestamp = "منذ ساعتين", BadgeClass = "badge-warning" },
        new() { Action = "اكتمل تشغيل خط أنابيب CI/CD", User = "النظام التلقائي", Timestamp = "منذ 3 ساعات", BadgeClass = "badge-info" }
    };
    public List<SystemMetricItem> ServerMetrics { get; set; } = new()
    {
        new() { Name = "استهلاك المعالج (CPU)", Usage = "24%", ProgressPercent = 24, StatusColor = "bg-emerald-500" },
        new() { Name = "استهلاك الذاكرة (RAM)", Usage = "4.2 GB / 8 GB (52%)", ProgressPercent = 52, StatusColor = "bg-blue-500" },
        new() { Name = "سعة التخزين (SSD)", Usage = "180 GB / 512 GB (35%)", ProgressPercent = 35, StatusColor = "bg-indigo-500" }
    };
}

public class RecentActivityItem
{
    public string Action { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
}

public class SystemMetricItem
{
    public string Name { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public string StatusColor { get; set; } = string.Empty;
}
