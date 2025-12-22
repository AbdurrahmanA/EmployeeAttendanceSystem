using EmployeeAttendanceSystem.Server.Application.Interfaces;
using EmployeeAttendanceSystem.Server.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EmployeeAttendanceSystem.Server.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key; 
        private readonly UserManager<Employee> _userManager; 

        public TokenService(IConfiguration config, UserManager<Employee> userManager)
        {
            _config = config;
            _userManager = userManager;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        }

        public async Task<string> CreateToken(Employee employee)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId, employee.Id), 
                new Claim(JwtRegisteredClaimNames.Email, employee.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, employee.UserName), 

                new Claim("name", employee.Name),
                new Claim("surname", employee.Surname)
            };

            var roles = await _userManager.GetRolesAsync(employee);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims), 
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds, 
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"] 
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}

