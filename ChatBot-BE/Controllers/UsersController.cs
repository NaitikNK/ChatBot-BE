using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatBot_BE.Services;
using ChatBot_BE.Dto;
using ChatBot_BE.Model;
using ChatBot_BE.Data;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserStore _users;
        private readonly AppDbContext _db;

        public UsersController(IUserStore users, AppDbContext db)
        {
            _users = users;
            _db = db;
        }

        /// <summary>
        /// Extracts the current user from the X-User-Id header and checks if they are an admin.
        /// </summary>
        private async Task<(User? user, bool isAdmin)> GetCurrentUserAsync()
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var val) || !int.TryParse(val, out var id))
                return (null, false);
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            return (user, user?.Role?.RoleName == "Admin");
        }

        /// <summary>
        /// Admin only — returns all users.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetAll()
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<List<UserResponse>> { Success = false, Error = "User context missing. Send X-User-Id header." });

            if (!isAdmin)
                return StatusCode(403, new ApiResponse<List<UserResponse>> { Success = false, Error = "Only admins can view all users." });

            var users = await _users.GetAllAsync();
            var responses = users.Select(u => ToResponse(u)).ToList();

            return Ok(new ApiResponse<List<UserResponse>> { Success = true, Data = responses });
        }

        /// <summary>
        /// Admin → any user. User → only own profile.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> Get(int id)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<UserResponse> { Success = false, Error = "User context missing. Send X-User-Id header." });

            if (!isAdmin && currentUser.Id != id)
                return StatusCode(403, new ApiResponse<UserResponse> { Success = false, Error = "You can only view your own profile." });

            var user = await _users.GetAsync(id);
            if (user == null) return NotFound(new ApiResponse<UserResponse> { Success = false, Error = "Not found." });
            return Ok(new ApiResponse<UserResponse> { Success = true, Data = ToResponse(user) });
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] UserCreateRequest request)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null || !isAdmin)
                return StatusCode(403, new ApiResponse<UserResponse> { Success = false, Error = "Only admins can create users." });

            try
            {
                var created = await _users.AddAsync(new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RoleId = request.RoleId,
                    OwnerSessionId = ""
                });

                return CreatedAtAction(nameof(Get), new { id = created.Id }, new ApiResponse<UserResponse> { Success = true, Data = ToResponse(created) });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<UserResponse> { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Admin → any user. User → only own profile.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UserUpdateRequest request)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<object> { Success = false, Error = "User context missing. Send X-User-Id header." });

            if (!isAdmin && currentUser.Id != id)
                return StatusCode(403, new ApiResponse<object> { Success = false, Error = "You can only update your own profile." });

            var existing = await _users.GetAsync(id);
            if (existing == null)
                return NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });

            if (existing.OwnerSessionId == "SEEDED_RECORD" && !isAdmin)
                return BadRequest(new ApiResponse<object> { Success = false, Error = "Cannot modify seeded users." });

            try
            {
                var ok = await _users.UpdateAsync(id, new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    RoleId = request.RoleId,
                    PasswordHash = request.Password != null ? BCrypt.Net.BCrypt.HashPassword(request.Password) : null
                });
                return Ok(new ApiResponse<object> { Success = ok });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Admin → any user. User → only own profile.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<object> { Success = false, Error = "User context missing. Send X-User-Id header." });

            if (!isAdmin && currentUser.Id != id)
                return StatusCode(403, new ApiResponse<object> { Success = false, Error = "You can only delete your own profile." });

            var existing = await _users.GetAsync(id);
            if (existing == null)
                return NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });

            if (existing.OwnerSessionId == "SEEDED_RECORD" && !isAdmin)
                return BadRequest(new ApiResponse<object> { Success = false, Error = "Cannot delete seeded users." });

            var ok = await _users.DeleteAsync(id);
            return Ok(new ApiResponse<object> { Success = ok });
        }

        /// <summary>
        /// Returns all users excluding admins. No auth required.
        /// </summary>
        [HttpGet("all-users")]
        public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetUsersExcludingAdmins()
        {
            var users = await _users.GetAllAsync();
            var filtered = users.Where(u => u.Role?.RoleName != "Admin").Select(u => ToResponse(u)).ToList();
            return Ok(new ApiResponse<List<UserResponse>> { Success = true, Data = filtered });
        }

        [HttpPost("{id}/change-password")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<object> { Success = false, Error = "User context missing. Send X-User-Id header." });

            if (!isAdmin && currentUser.Id != id)
                return StatusCode(403, new ApiResponse<object> { Success = false, Error = "You can only change your own password." });

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new ApiResponse<object> { Success = false, Error = "Password is required." });

            var existing = await _users.GetAsync(id);
            if (existing == null) return NotFound();

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            existing.PasswordHash = newHash;
            existing.UpdatedAt = DateTime.UtcNow;

            var ok = await _users.UpdateAsync(id, existing);
            return Ok(new ApiResponse<object> { Success = ok });
        }

        private UserResponse ToResponse(User u)
        {
            return new UserResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.Role?.RoleName,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                IsDefault = u.OwnerSessionId == "SEEDED_RECORD"
            };
        }
    }
}
