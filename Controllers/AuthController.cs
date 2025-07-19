using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManager.Data;
using TaskManager.Dtos;
using TaskManager.Helper;
using TaskManager.Helpers;
using TaskManager.Model;

namespace TaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TaskDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(TaskDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/signup
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] UserSignupDto signupDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == signupDto.Email))
            {
                return BadRequest("Email already in use.");
            }

            var user = new User
            {
                Email = signupDto.Email,
                PasswordHash = HashPassword(signupDto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            var hashed = HashPassword(loginDto.Password);
            if (user.PasswordHash != hashed)
                return Unauthorized("Invalid email or password.");

            var token = JwtHelper.GenerateToken(user, _configuration);

            var refreshToken = RefreshTokenHelper.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = token,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequest)
        {
            var principal = JwtHelper.GetPrincipalFromExpiredToken(tokenRequest.Token, _configuration);
            if (principal == null)
                return BadRequest("Invalid access token or refresh token.");

            var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return BadRequest("Invalid token claims.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.RefreshToken != tokenRequest.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Invalid or expired refresh token.");

            var newJwtToken = JwtHelper.GenerateToken(user, _configuration);
            var newRefreshToken = RefreshTokenHelper.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = newJwtToken,
                RefreshToken = newRefreshToken
            });
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Get the userId from the token claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Find the user in the database
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();

            // Invalidate the refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
