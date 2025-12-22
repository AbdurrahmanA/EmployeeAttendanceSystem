using EmployeeAttendanceSystem.Server.API.Filters;
using EmployeeAttendanceSystem.Server.Context;
using EmployeeAttendanceSystem.Server.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace EmployeeAttendanceSystem.Server.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public ReportController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("admin/all-logs")]
        public async Task<IActionResult> GetAdminAllLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.AttendanceLogs
                                .Include(log => log.Employee)
                                .AsNoTracking()
                                .AsQueryable();

            if (startDate.HasValue) query = query.Where(log => log.CheckInTime.Date >= startDate.Value.Date);
            if (endDate.HasValue)
            {
                var exclusiveEndDate = endDate.Value.Date.AddDays(1);
                query = query.Where(log => log.CheckInTime < exclusiveEndDate);
            }

            var now = DateTime.UtcNow.AddHours(3);

            var reportData = await query
                .OrderByDescending(log => log.CheckInTime)
                .Select(log => new
                {
                    log.LogID,
                    UserId = log.Employee.Id,
                    log.CheckInTime,
                    log.CheckOutTime,

                    DurationHours = log.CheckOutTime.HasValue
                                    ? (log.CheckOutTime.Value - log.CheckInTime).TotalHours
                                    : (now - log.CheckInTime).TotalHours,

                    EmployeeInfo = new
                    {
                        log.Employee.Id,
                        EmployeeName = log.Employee.Name + " " + log.Employee.Surname,
                        log.Employee.Email,
                        log.Employee.Department
                    }
                })
                .ToListAsync();

            return Ok(reportData);
        }

        [HttpGet("admin/download-report")]
        [ServiceFilter(typeof(AuditActionFilter))]

        public async Task<IActionResult> DownloadAdminReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            ExcelPackage.License.SetNonCommercialPersonal("<Abdurrahman>");

            var query = _context.AttendanceLogs
                .Include(log => log.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue) query = query.Where(log => log.CheckInTime.Date >= startDate.Value.Date);
            if (endDate.HasValue)
            {
                var exclusiveEndDate = endDate.Value.Date.AddDays(1);
                query = query.Where(log => log.CheckInTime < exclusiveEndDate);
            }

            var now = DateTime.UtcNow.AddHours(3);

            var reportData = await query
                .OrderByDescending(log => log.CheckInTime)
                .Select(log => new
                {
                    UserId = log.Employee.Id,
                    Calisan = log.Employee.Name + " " + log.Employee.Surname,
                    Departman = log.Employee.Department,
                    log.Employee.Email,
                    GirisZamani = log.CheckInTime,
                    CikisZamani = log.CheckOutTime,

                    DurationHours = log.CheckOutTime.HasValue
                                    ? (log.CheckOutTime.Value - log.CheckInTime).TotalHours
                                    : (now - log.CheckInTime).TotalHours
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Mesai Raporu");

            worksheet.Cells[1, 1].Value = "Kullanıcı ID";
            worksheet.Cells[1, 2].Value = "Çalışan";
            worksheet.Cells[1, 3].Value = "Departman";
            worksheet.Cells[1, 4].Value = "E-posta";
            worksheet.Cells[1, 5].Value = "Giriş Zamanı";
            worksheet.Cells[1, 6].Value = "Çıkış Zamanı";
            worksheet.Cells[1, 7].Value = "Çalışılan Süre";
            worksheet.Cells[1, 8].Value = "Kalan Mesai (9s)";

            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            int row = 2;
            foreach (var item in reportData)
            {
                worksheet.Cells[row, 1].Value = item.UserId;

                worksheet.Cells[row, 2].Value = item.Calisan;
                worksheet.Cells[row, 3].Value = item.Departman ?? "-";

                worksheet.Cells[row, 4].Value = item.Email;

                worksheet.Cells[row, 5].Value = item.GirisZamani;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "dd.MM.yyyy HH:mm";

                if (item.CikisZamani.HasValue)
                {
                    worksheet.Cells[row, 6].Value = item.CikisZamani;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "dd.MM.yyyy HH:mm";
                }
                else
                {
                    worksheet.Cells[row, 6].Value = "Aktif (İçeride)";
                    worksheet.Cells[row, 6].Style.Font.Color.SetColor(Color.Green);
                }

                if (item.DurationHours > 0)
                {
                    TimeSpan worked = TimeSpan.FromHours(item.DurationHours);
                    worksheet.Cells[row, 7].Value = $"{worked.Hours} sa {worked.Minutes} dk";
                }
                else
                {
                    worksheet.Cells[row, 7].Value = "-";
                }

                double remaining = 9.0 - item.DurationHours;

                if (remaining > 0) 
                {
                    TimeSpan remSpan = TimeSpan.FromHours(remaining);
                    worksheet.Cells[row, 8].Value = $"{remSpan.Hours} sa {remSpan.Minutes} dk kaldı";
                    worksheet.Cells[row, 8].Style.Font.Color.SetColor(Color.Blue);
                }
                else 
                {
                    TimeSpan overSpan = TimeSpan.FromHours(Math.Abs(remaining));
                    if (overSpan.TotalMinutes > 1)
                    {
                        worksheet.Cells[row, 8].Value = $"Tamamlandı (+{overSpan.Hours} sa {overSpan.Minutes} dk)";
                    }
                    else
                    {
                        worksheet.Cells[row, 8].Value = "Tamamlandı";
                    }
                    worksheet.Cells[row, 8].Style.Font.Color.SetColor(Color.Green);
                    worksheet.Cells[row, 8].Style.Font.Bold = true;
                }

                row++;
            }

            worksheet.Cells.AutoFitColumns();

            var excelBytes = package.GetAsByteArray();
            var fileName = $"MesaiRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}