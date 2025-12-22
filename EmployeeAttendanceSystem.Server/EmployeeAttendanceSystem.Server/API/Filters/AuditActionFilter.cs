using EmployeeAttendanceSystem.Server.Application.DTOs;
using EmployeeAttendanceSystem.Server.Context;
using EmployeeAttendanceSystem.Server.Domain;
using EmployeeAttendanceSystem.Server.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeAttendanceSystem.Server.API.Filters
{
    public class AuditActionFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public AuditActionFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var startTime = DateTime.UtcNow.AddHours(3);

            var actionName = context.ActionDescriptor.RouteValues["action"]?.ToString();

            var actionsToLog = new[] { "Login", "GetAdminAllLogs", "DownloadAdminReport", "GetLogs", "ExportLogs" };

            if (actionName == null || !actionsToLog.Contains(actionName))
            {
                await next();
                return;
            }

            string? userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? userName = httpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(userName) && actionName == "Login")
            {
                if (context.ActionArguments.TryGetValue("loginDto", out var dtoObj) && dtoObj is LoginDto loginDto)
                {
                    userName = loginDto.Email; 

                    var user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                    if (user != null)
                    {
                        userId = user.Id;
                    }
                    else
                    {
                        userId = "Hatalı E-posta";
                    }
                }
            }

            string? ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            string action = $"{httpContext.Request.Method} {httpContext.Request.Path}";

            var resultContext = await next();

            bool success = resultContext.Exception == null;
            string? errorMessage = resultContext.Exception?.Message;

            if (resultContext.Result is ObjectResult objResult && objResult.StatusCode >= 400)
            {
                success = false;
                errorMessage ??= $"HTTP {objResult.StatusCode}";
                if (objResult.Value != null) errorMessage += $" - {objResult.Value}";
            }
            else if (resultContext.Result is StatusCodeResult scResult && scResult.StatusCode >= 400)
            {
                success = false;
                errorMessage ??= $"HTTP {scResult.StatusCode}";
            }

            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName ?? "Anonim",
                Action = action,
                Timestamp = startTime,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                ErrorMessage = errorMessage,
                EntityType = null,
                EntityId = null,
                OldValues = null,
                NewValues = null
            };

            try
            {
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audit log hatası: {ex.Message}");
            }
        }
    }
}