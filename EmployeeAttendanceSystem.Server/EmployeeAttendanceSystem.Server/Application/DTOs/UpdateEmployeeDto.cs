using System.ComponentModel.DataAnnotations;

namespace EmployeeAttendanceSystem.Server.Application.DTOs
{
    public class UpdateEmployeeDto
    {
        [Required(ErrorMessage = "E-posta alanı gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "İsim alanı gereklidir")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Soyisim alanı gereklidir")]
        public required string Surname { get; set; }

        public string? Department { get; set; }

        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string? Password { get; set; }
    }
}
