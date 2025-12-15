using EmployeeAttendanceSystem.Server.Domain;

namespace EmployeeAttendanceSystem.Server.Services
{
    public interface ITokenService
    {
        Task<string> CreateToken(Employee employee);
    }
}
