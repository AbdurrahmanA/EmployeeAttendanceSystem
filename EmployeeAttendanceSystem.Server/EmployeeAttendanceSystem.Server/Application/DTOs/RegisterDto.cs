using System.ComponentModel.DataAnnotations;

namespace EmployeeAttendanceSystem.Server.Application.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "E-posta alanı gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public required string Email { get; set; }
        [Required(ErrorMessage = "Şifre alanı gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public required string Password { get; set; }
        [Required(ErrorMessage = "İsim alanı gereklidir")]
        public required string Name { get; set; }
        [Required(ErrorMessage = "Soyisim alanı gereklidir")]

        public required string Surname { get; set; }
        public string? Department { get; set; }
    }
}
