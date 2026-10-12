using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using RootHomeController = MyDevOpsApp.Controllers.HomeController;
using AdminHomeController = MyDevOpsApp.Areas.Admin.Controllers.HomeController;
using AdminDashboardController = MyDevOpsApp.Areas.Admin.Controllers.DashboardController;
using AdminPayrollController = MyDevOpsApp.Areas.Admin.Controllers.PayrollController;
using EmployeesHomeController = MyDevOpsApp.Areas.Employees.Controllers.HomeController;
using MyDevOpsApp.Areas.Admin.Models;
using MyDevOpsApp.Areas.Employees.Models;
using MyDevOpsApp.Models;
using MyDevOpsApp.Services;
using Xunit;

namespace MyDevOpsApp.Tests;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsViewResult()
    {
        // Arrange
        var logger = NullLogger<RootHomeController>.Instance;
        var controller = new RootHomeController(logger);

        // Act
        var result = controller.Index();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        // Arrange
        var logger = NullLogger<RootHomeController>.Instance;
        var controller = new RootHomeController(logger);

        // Act
        var result = controller.Privacy();

        // Assert
        Assert.IsType<ViewResult>(result);
    }
}

public class AdminAreaTests
{
    [Fact]
    public void HomeController_Index_RedirectsToDashboard()
    {
        // Arrange
        var controller = new AdminHomeController();

        // Act
        var result = controller.Index();

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
    }

    [Fact]
    public void DashboardIndex_RedirectsToPayrollReport()
    {
        // Arrange
        var controller = new AdminDashboardController();

        // Act
        var result = controller.Index();

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Report", redirect.ActionName);
        Assert.Equal("Payroll", redirect.ControllerName);
    }
}

public class AccountModelTests
{
    [Fact]
    public void LoginViewModel_InitializesPropertiesCorrectly()
    {
        var model = new LoginViewModel
        {
            Email = "admin@mydevopsapp.com",
            Password = "Admin@123456",
            RememberMe = true
        };

        Assert.Equal("admin@mydevopsapp.com", model.Email);
        Assert.Equal("Admin@123456", model.Password);
        Assert.True(model.RememberMe);
    }

    [Fact]
    public void RegisterEmployeeViewModel_InitializesPropertiesCorrectly()
    {
        var model = new RegisterEmployeeViewModel
        {
            NationalId = "29010101234567",
            PaymentNumber = "1055",
            Email = "emp@company.com",
            Password = "Password123"
        };

        Assert.Equal("29010101234567", model.NationalId);
        Assert.Equal("1055", model.PaymentNumber);
        Assert.Equal("emp@company.com", model.Email);
    }
}

public class PayrollParserTests
{
    [Fact]
    public void PayrollUploadViewModel_DefaultValues_AreValid()
    {
        var model = new PayrollUploadViewModel
        {
            Year = 2026,
            Month = 9,
            PayrollType = "مرتبات"
        };

        Assert.Equal(2026, model.Year);
        Assert.Equal(9, model.Month);
        Assert.Equal("مرتبات", model.PayrollType);
    }

    [Fact]
    public void ParseExcel_WithSampleFile_ExtractsEmployeesSuccessfully()
    {
        string samplePath = @"D:\Personal\مراجعه صرفيه 9-2026.xlsx";
        if (!File.Exists(samplePath))
        {
            // إذا لم يكن الملف متوفراً في بيئة أخرى كـ CI
            return;
        }

        using var stream = File.OpenRead(samplePath);
        var parser = new ExcelPayrollParserService(null!, NullLogger<ExcelPayrollParserService>.Instance);
        var result = parser.ParseExcel(stream);

        Assert.NotEmpty(result);
        var firstSlip = result[0];
        Assert.False(string.IsNullOrWhiteSpace(firstSlip.EmployeeName));
        Assert.False(string.IsNullOrWhiteSpace(firstSlip.NationalId));
        Assert.True(firstSlip.Earnings.Count > 0);
        Assert.True(firstSlip.TotalEarnings > 0);
    }
}
