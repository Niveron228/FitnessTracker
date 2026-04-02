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
        // DI - _context - db, _config - configuration obj appsettings.json
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        // DI Constructor
        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        // register method
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            // looking for any user with such an email
            if (await _context.Users.AnyAsync(u => u.email == request.email))
            {
                // if found return badrequest
                return BadRequest("User with this email already exist.");
            }
            // if its new user with new email - encrypt his password and save into passwordHash variable

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.password);

            string assignedRole = "User";

            if(request.email.ToLower() == "admin123@test.com")
            {
                assignedRole = "Admin";
            }

            // create new user with that email and hashed password
            var user = new Users
            {
                email = request.email,
                passwordHash = passwordHash,
                Role = assignedRole
            };

            // add new user in table save and return success code status

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Success Registration!, Your role is: {user.Role}" });
        }

        [HttpPost("login")]

        // login method
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            // check user in db
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == request.email);
            if(user == null)
            {
                // if not found - return badrequest
                return BadRequest("Incorrect email");
            }
            // found
            // checking entered password with encrypted passwordHash from Users table

            if(!BCrypt.Net.BCrypt.Verify(request.password, user.passwordHash))
            {
                // if incorrect - return badreques
                return BadRequest("Incorrect Email or password!");
            }

            // creating token if success and return success status code

            string token = CreateToken(user);

            // token settings for cookies
            var cookieOption = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.Now.AddDays(1)
            };

            // putting token in cookies
            Response.Cookies.Append("jwt", token, cookieOption);

            return Ok(new { message = $"Welcome back {user.email}!, your token saved automatically in cookies!" });
        }

        // create token method
        private string CreateToken(Users user)
        {
            // Jwt:Token from _config (appsettings.json)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Token"]));

            // credentials
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            // hiding translated using Base64 user id and email in token a
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Email,user.email),
                new Claim(ClaimTypes.Role,user.Role)
            };

            // creating token
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );

            // returning token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
