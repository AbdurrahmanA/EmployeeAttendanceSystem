using EmployeeAttendanceSystem.Server.Domain.Entities;

namespace EmployeeAttendanceSystem.Server.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateToken(Employee employee);
    }
}
