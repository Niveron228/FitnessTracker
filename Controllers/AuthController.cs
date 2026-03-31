using CaloriesTracker.DB;
using Microsoft.AspNetCore.Mvc;
using CaloriesTracker.DTOs;
using CaloriesTracker.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace CaloriesTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.email == request.email))
            {
                return BadRequest("User with this email already exist.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.password);

            var user = new Users
            {
                email = request.email,
                passwordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Success Registration!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == request.email);
            if(user == null)
            {
                return BadRequest("Incorrect email");
            }

            if(!BCrypt.Net.BCrypt.Verify(request.password, user.passwordHash))
            {
                return BadRequest("Incorrect Email or password!");
            }

            string token = CreateToken(user);

            return Ok(new { message = $"Welcome back {user.email}!, your token: {token}" });
        }

        private string CreateToken(Users user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Token"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Email,user.email)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
