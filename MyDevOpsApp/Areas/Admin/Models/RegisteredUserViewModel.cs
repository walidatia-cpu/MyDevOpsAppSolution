namespace MyDevOpsApp.Areas.Admin.Models;

public class RegisteredUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? PaymentNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string Role { get; set; } = "Employee";
    public bool IsTwoFactorEnabled { get; set; }
    public bool EmailConfirmed { get; set; }

    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm");

    public string LastLoginFormatted => LastLoginAt.HasValue
        ? LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm")
        : "لم يسجل دخول بعد";

    public string LastLoginTimeAgo
    {
        get
        {
            if (!LastLoginAt.HasValue) return "لم يسجل دخول بعد";
            var diff = DateTime.Now - LastLoginAt.Value;

            if (diff.TotalMinutes < 2) return "نشط الآن 🟢";
            if (diff.TotalMinutes < 60) return $"منذ {(int)diff.TotalMinutes} دقيقة";
            if (diff.TotalHours < 24) return $"منذ {(int)diff.TotalHours} ساعة";
            if (diff.TotalDays < 30) return $"منذ {(int)diff.TotalDays} يوم";
            if (diff.TotalDays < 365) return $"منذ {(int)(diff.TotalDays / 30)} شهر";
            return $"منذ {(int)(diff.TotalDays / 365)} سنة";
        }
    }

    public bool IsRecentlyActive => LastLoginAt.HasValue && (DateTime.Now - LastLoginAt.Value).TotalDays <= 7;
}

public class UsersListReportViewModel
{
    public List<RegisteredUserViewModel> Users { get; set; } = new();
    public int TotalUsersCount => Users.Count;
    public int AdminsCount => Users.Count(u => u.Role == "Admin");
    public int EmployeesCount => Users.Count(u => u.Role == "Employee");
    public int ActiveRecentlyCount => Users.Count(u => u.IsRecentlyActive);
    public int NeverLoggedInCount => Users.Count(u => !u.LastLoginAt.HasValue);
}
