namespace EmployeeAttendanceSystem.Server.Application.DTOs
{
    public class DailyAttendanceSummaryDto
    {
        public double CompletedHours { get; set; }
        public double CurrentSessionHours { get; set; }
        public double RemainingHours { get; set; }
        public string RemainingTimeText { get; set; }
        public bool IsCheckedIn { get; set; }
    }
}
