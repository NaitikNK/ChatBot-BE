using ChatBot_BE.Model;
using ChatBot_BE.Services;
using ChatBot_BE.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserStore _userStore;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserStore userStore, AppDbContext context, ILogger<AuthController> logger)
        {
            _userStore = userStore;
            _context = context;
            _logger = logger;
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                return BadRequest(new { message = "Email and password are required." });

            var user = await _userStore.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed for {Email}: User not found in database.", loginDto.Email);
                return BadRequest(new { message = "User with this email not found." });
            }

            if (user.PasswordHash == null)
            {
                _logger.LogWarning("Login failed for {Email}: User has no password hash set.", loginDto.Email);
                return BadRequest(new { message = "User account has no password set. Please reset your password." });
            }

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for {Email}: BCrypt verification failed (password mismatch).", loginDto.Email);
                return BadRequest(new { message = "Incorrect password. Please try again." });
            }

            _logger.LogInformation("Login successful for user {Email}.", loginDto.Email);
            return Ok(new
            {
                token = "dummy-token-which-front-end-stores",
                user = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role?.RoleName // Use RoleName from navigation property
                }
            });
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupDto signupDto)
        {
            if (string.IsNullOrWhiteSpace(signupDto.Email) || string.IsNullOrWhiteSpace(signupDto.Password) ||
                string.IsNullOrWhiteSpace(signupDto.FirstName) || string.IsNullOrWhiteSpace(signupDto.LastName))
                return BadRequest(new { message = "All fields are required." });

            var existingUser = await _userStore.GetByEmailAsync(signupDto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already in use." });

            // Get default 'User' role
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (userRole == null)
                return StatusCode(500, new { message = "System roles not initialized." });

            var newUser = new User
            {
                FirstName = signupDto.FirstName,
                LastName = signupDto.LastName,
                Email = signupDto.Email,
                RoleId = userRole.Id,
                OwnerSessionId = Guid.NewGuid().ToString(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(signupDto.Password)
            };

            await _userStore.AddAsync(newUser);

            return Ok(new { message = "Signup successful. You can now login." });
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpGet("profile-test")]
        public IActionResult TestProfile() => Ok(new { message = "Auth controller profile route is alive!" });

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            // Extract User ID from headers (simulated auth)
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdVal) || !int.TryParse(userIdVal, out var userId))
                return Unauthorized(new { message = "User context missing." });

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // Admin check - block edits for admins
            if (user.Role?.RoleName == "Admin")
                return BadRequest(new { message = "Admin profile cannot be edited." });

            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                user = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role?.RoleName
                }
            });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SignupDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
