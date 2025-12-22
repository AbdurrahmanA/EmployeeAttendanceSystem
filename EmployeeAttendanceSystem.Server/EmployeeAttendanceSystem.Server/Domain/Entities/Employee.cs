using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EmployeeAttendanceSystem.Server.Domain.Entities
{
    public class Employee : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [Required]
        [StringLength(50)]
        public string Surname { get; set; }
        public string? Department { get; set; }
        public virtual ICollection<AttendanceLog> Logs { get; set; }
        public Employee()
        {
            Logs = new HashSet<AttendanceLog>();
        }
    }
}
