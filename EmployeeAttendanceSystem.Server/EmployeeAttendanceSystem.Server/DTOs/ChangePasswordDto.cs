using System.ComponentModel.DataAnnotations;

namespace EmployeeAttendanceSystem.Server.DTOs
{
    public class ChangePasswordDto
    {
        public required string OldPassword { get; set; }
        [MinLength(6, ErrorMessage ="Yeni şifre en az 6 karakter içermelidir.")]
        public required string NewPassword { get; set; }
        [Required(ErrorMessage = "Şifreyi doğrulamak zorunludur.")]
        [Compare("NewPassword",ErrorMessage ="Şifreler eşleşmiyor")]
        public required string ConfirmNewPassword { get; set; }
    }
}
