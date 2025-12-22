using EmployeeAttendanceSystem.Server.Application.DTOs;
using EmployeeAttendanceSystem.Server.Context;
using EmployeeAttendanceSystem.Server.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeAttendanceSystem.Server.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly UserManager<Employee> _userManager;
        private readonly ApplicationDbContext _context;

        public AttendanceController(UserManager<Employee> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("summary")]
        [Authorize]
        public async Task<IActionResult> GetDailySummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var now = DateTime.UtcNow.AddHours(3);
            var todayStart = now.Date;

            var dailyTargetHours = 9.0;

            var todaysLogs = await _context.AttendanceLogs
                .Where(l => l.EmployeeID == userId && l.CheckInTime >= todayStart)
                .ToListAsync();

            double completedHours = todaysLogs
                .Where(l => l.CheckOutTime != null)
                .Sum(l => (l.CheckOutTime.Value - l.CheckInTime).TotalHours);

            var activeLog = todaysLogs.FirstOrDefault(l => l.CheckOutTime == null);
            double currentSessionHours = 0;

            if (activeLog != null)
            {
                currentSessionHours = (now - activeLog.CheckInTime).TotalHours;
            }

            double totalWorked = completedHours + currentSessionHours;
            double remainingHours = dailyTargetHours - totalWorked;

            if (remainingHours < 0) remainingHours = 0;

            TimeSpan t = TimeSpan.FromHours(remainingHours);
            string remainingTimeText = string.Format("{0:D2} saat {1:D2} dk.", t.Hours, t.Minutes);

            return Ok(new DailyAttendanceSummaryDto
            {
                CompletedHours = completedHours,
                CurrentSessionHours = currentSessionHours,
                RemainingHours = remainingHours,
                RemainingTimeText = remainingTimeText,
                IsCheckedIn = activeLog != null
            });
        }

        [Authorize]
        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn()
        {
            var loggedInUserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(loggedInUserID))
                return Unauthorized(new { Message = "Oturum Geçersizdir." });

            var userExists = await _userManager.Users.AnyAsync(user => user.Id == loggedInUserID);
            if (!userExists)
            {
                return Unauthorized(new { Message = "Kullanıcı sistemden silinmiştir." });
            }

            var openLog = await _context.AttendanceLogs
                .Where(log => log.EmployeeID == loggedInUserID && log.CheckOutTime == null)
                .OrderByDescending(log => log.CheckInTime)
                .FirstOrDefaultAsync();

            if (openLog != null && openLog.CheckInTime.Date != DateTime.UtcNow.AddHours(3).Date)
            {
                openLog.CheckOutTime = openLog.CheckInTime.Date.AddDays(1).AddTicks(-1);
                await _context.SaveChangesAsync();
            }
            else if (openLog != null)
            {
                return BadRequest(new { Message = "Zaten bugün için açık bir mesai kaydınız bulunuyor." });
            }

            var newLog = new AttendanceLog
            {
                EmployeeID = loggedInUserID,
                CheckInTime = DateTime.UtcNow.AddHours(3), 
            };

            _context.AttendanceLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Mesai başarıyla başlatıldı" });
        }

        [Authorize]
        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut()
        {
            var loggedInUserID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userExists = await _userManager.Users.AnyAsync(user => user.Id == loggedInUserID);
            if (!userExists)
            {
                return Unauthorized(new { Message = "Kullanıcı sistemden silinmiştir." });
            }

            var openLog = await _context.AttendanceLogs
                .Where(log => log.EmployeeID == loggedInUserID && log.CheckOutTime == null)
                .OrderByDescending(log => log.CheckInTime)
                .FirstOrDefaultAsync();

            if (openLog == null)
            {
                return BadRequest(new { Message = "Açık bir mesai başlangıcınız bulunmuyor." });
            }

            openLog.CheckOutTime = DateTime.UtcNow.AddHours(3); 
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Mesai başarıyla bitirildi" });
        }
    }
}