using EmployeeAttendanceSystem.Server.API.Filters;
using EmployeeAttendanceSystem.Server.Context;
using EmployeeAttendanceSystem.Server.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<AuditLog> ApplyActionFilter(IQueryable<AuditLog> query, string action)
        {
            return action switch
            {
                "Login" => query.Where(a => a.Action.Contains("api/account/login")),

                "Insert" => query.Where(a => a.Action == "Insert"),
                "Update" => query.Where(a => a.Action == "Update"),
                "Delete" => query.Where(a => a.Action == "Delete"),

                _ => query.Where(a => a.Action == action)
            };
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? userName,
            [FromQuery] string? action,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp.Date <= endDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(a =>
                    a.UserId != null && a.UserId.Contains(userName) ||
                    a.UserName != null && a.UserName.Contains(userName)
                );
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = ApplyActionFilter(query, action);
            }

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.UserName,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.Timestamp,
                    a.Success,
                    a.IpAddress,
                    a.UserAgent,
                    a.ErrorMessage,
                    a.OldValues,
                    a.NewValues
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = logs
            });
        }

        [HttpGet("logs/{id}")]
        public async Task<IActionResult> GetLogDetail(int id)
        {
            var log = await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (log == null)
            {
                return NotFound(new { message = "Log bulunamadı" });
            }
            return Ok(log);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);

            var stats = new
            {
                TotalLast24Hours = await _context.AuditLogs.CountAsync(a => a.Timestamp >= last24Hours),

                SuccessfullLast24Hours = await _context.AuditLogs.CountAsync(a => a.Timestamp >= last24Hours && a.Success),

                FailedLast24Hours = await _context.AuditLogs.CountAsync(a => a.Timestamp >= last24Hours && !a.Success),

                MostActiveUser = await _context.AuditLogs
                    .Where(a => a.Timestamp >= last24Hours && a.UserId != null)
                    .GroupBy(a => new { a.UserId, a.UserName })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.UserName,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefaultAsync(),

                ActionDistribution = await _context.AuditLogs
                    .Where(a => a.Timestamp >= last24Hours)
                    .GroupBy(a => a.Action)
                    .Select(g => new
                    {
                        Action = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync()
            };
            return Ok(stats);
        }

        [HttpGet("entity-history/{entityType}/{entityId}")]
        public async Task<IActionResult> GetEntityHistory(string entityType, string entityId)
        {
            var history = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.UserName,
                    a.Action,
                    a.Timestamp,
                    a.OldValues,
                    a.NewValues
                })
                .ToListAsync();

            if (!history.Any())
            {
                return NotFound(new { message = "Log geçmişi bulunamadı" });
            }
            return Ok(history);
        }

        [HttpGet("export")]
        [ServiceFilter(typeof(AuditActionFilter))]

        public async Task<IActionResult> ExportLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? userName,
            [FromQuery] string? action)
        {
            ExcelPackage.License.SetNonCommercialPersonal("<Abdurrahman>");

            var query = _context.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp.Date <= endDate.Value.Date);
            }
            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(a =>
                    a.UserId != null && a.UserId.Contains(userName) ||
                    a.UserName != null && a.UserName.Contains(userName)
                );
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = ApplyActionFilter(query, action);
            }

            var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Denetim Logları");

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Kullanıcı";
            worksheet.Cells[1, 3].Value = "Kullanıcı ID";
            worksheet.Cells[1, 4].Value = "İşlem";
            worksheet.Cells[1, 5].Value = "Varlık Tipi";
            worksheet.Cells[1, 6].Value = "Varlık ID";
            worksheet.Cells[1, 7].Value = "Tarih/Saat";
            worksheet.Cells[1, 8].Value = "Başarılı";
            worksheet.Cells[1, 9].Value = "IP Adresi";
            worksheet.Cells[1, 10].Value = "Hata Mesajı";
            worksheet.Cells[1, 11].Value = "Eski Değerler";
            worksheet.Cells[1, 12].Value = "Yeni Değerler";

            using (var range = worksheet.Cells[1, 1, 1, 12])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            int row = 2;
            foreach (var log in logs)
            {
                worksheet.Cells[row, 1].Value = log.Id;
                worksheet.Cells[row, 2].Value = log.UserName ?? "Anonim";
                worksheet.Cells[row, 3].Value = log.UserId;
                worksheet.Cells[row, 4].Value = log.Action;
                worksheet.Cells[row, 5].Value = log.EntityType;
                worksheet.Cells[row, 6].Value = log.EntityId;

                worksheet.Cells[row, 7].Value = log.Timestamp;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "dd.mm.yyyy hh:mm:ss";

                worksheet.Cells[row, 8].Value = log.Success ? "Evet" : "Hayır";
                worksheet.Cells[row, 9].Value = log.IpAddress;
                worksheet.Cells[row, 10].Value = log.ErrorMessage;
                worksheet.Cells[row, 11].Value = log.OldValues;
                worksheet.Cells[row, 12].Value = log.NewValues;
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            var excelBytes = package.GetAsByteArray();
            var fileName = $"DenetimLoglari_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}