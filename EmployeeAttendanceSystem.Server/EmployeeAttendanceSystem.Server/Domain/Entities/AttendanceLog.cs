using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace EmployeeAttendanceSystem.Server.Domain.Entities
{
    public class AttendanceLog
    {
        [Key]
        public Guid LogID { get; set; } = Guid.NewGuid(); 
        public string EmployeeID { get; set; } 
        [Required] 
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        [ForeignKey("EmployeeID")]
        public virtual Employee Employee { get; set; }
        [NotMapped]
        public TimeSpan? Duration
        {
            get
            {
                if (CheckOutTime.HasValue)
                {
                    return CheckOutTime.Value - CheckInTime;
                }
                return null;
            }
        }
    }
}
