namespace MyDevOpsApp.Areas.Employees.Models;

public class TaskItemModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "عادية";
    public string Status { get; set; } = "قيد الانتظار";
    public string DueTime { get; set; } = string.Empty;
}

public class LeaveRequestModel
{
    public int Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
}
