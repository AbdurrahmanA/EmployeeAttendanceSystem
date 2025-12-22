using EmployeeAttendanceSystem.Server.API.Filters;
using EmployeeAttendanceSystem.Server.Application.DTOs;
using EmployeeAttendanceSystem.Server.Application.Interfaces;
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
    public class AccountController : ControllerBase
    {
        private readonly UserManager<Employee> _userManager;
        private readonly SignInManager<Employee> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<Employee> userManager,
            SignInManager<Employee> signInManager,
            ITokenService tokenService, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _context = context;
        }


        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var existingEmail = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingEmail != null)
            {
                return BadRequest(new[] { new { message = "Bu e-posta adresi zaten kullanılıyor." } });
            }

            var newEmployee = new Employee
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                Name = registerDto.Name,
                Surname = registerDto.Surname,
                Department = registerDto.Department
            };

            var result = await _userManager.CreateAsync(newEmployee, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRoleAsync(newEmployee, "Employee");
            return Ok(new { Message = "Kullanıcı başarıyla oluşturuldu." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ServiceFilter(typeof(AuditActionFilter))]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Unauthorized("Geçersiz e-posta veya şifre.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                loginDto.Password,
                false
            );

            if (!result.Succeeded)
            {
                return Unauthorized("Geçersiz e-posta veya şifre.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.CreateToken(user);

            var userDto = new UserDto
            {
                Email = user.Email!,
                Name = user.Name,
                Surname = user.Surname,
                Token = token,
                Role = roles.FirstOrDefault() ?? "Employee",
                Department = user.Department
            };

            return Ok(userDto);
        }

        [HttpPost("logout")]
        [Authorize]
        [ServiceFilter(typeof(AuditActionFilter))]
        public IActionResult Logout()
        {
            return Ok(new { message = "Başarıyla çıkış yaptınız." });
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.Surname,
                    u.Department
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Surname,
                user.Department
            });
        }
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }


            return Ok(new { message = "Kullanıcı başarıyla silindi." });
        }
        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        [ServiceFilter(typeof(AuditActionFilter))] 
        public async Task<IActionResult> UpdateUser(string id, UpdateEmployeeDto model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

            user.Name = model.Name;
            user.Surname = model.Surname;
            user.Department = model.Department;

            if (model.Email != user.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null && emailExists.Id != id)
                {
                    return BadRequest(new { message = "Bu e-posta adresi başka bir kullanıcıda kayıtlı." });
                }
                user.Email = model.Email;
                user.UserName = model.Email;
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (!passwordResult.Succeeded)
                {
                    return BadRequest(passwordResult.Errors);
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Kullanıcı başarıyla güncellendi." });
        }
        [HttpPost("change-password")]
        [Authorize]
        [ServiceFilter(typeof(AuditActionFilter))]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if(!result.Succeeded)
           {
                return BadRequest(result.Errors);
            }
            return Ok(new { Message = "Şifreniz başarıyla değiştirildi" });
        }
        [HttpGet("profile")]
        [Authorize] 
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            return Ok(new
            {
                user.Email,
                user.Name,
                user.Surname,
                user.Department
            });
        }
    }
}