namespace EmployeeAttendanceSystem.Server.Application.DTOs
{
    public class UserDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Role { get; set; }
        public string? Department { get; set; }
    }
}
