using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetAll()
        {
            var users = await _users.GetAllAsync();
            var responses = users.Select(u => ToResponse(u)).ToList();

            return Ok(new ApiResponse<List<UserResponse>> { Success = true, Data = responses });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> Get(int id)
        {
            var user = await _users.GetAsync(id);
            if (user == null) return NotFound(new ApiResponse<UserResponse> { Success = false, Error = "Not found." });
            return Ok(new ApiResponse<UserResponse> { Success = true, Data = ToResponse(user) });
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] UserCreateRequest request)
        {
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

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UserUpdateRequest request)
        {
            var existing = await _users.GetAsync(id);
            if (existing != null && existing.OwnerSessionId == "SEEDED_RECORD")
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

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var existing = await _users.GetAsync(id);
            if (existing != null && existing.OwnerSessionId == "SEEDED_RECORD")
                return BadRequest(new ApiResponse<object> { Success = false, Error = "Cannot delete seeded users." });

            var ok = await _users.DeleteAsync(id);
            return Ok(new ApiResponse<object> { Success = ok });
        }

        [HttpGet("all-users")]
        public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetUsersExcludingAdmins()
        {
            var users = await _users.GetAllAsync();
            // Assuming "Admin" role is to be excluded.
            var filtered = users.Where(u => u.Role?.RoleName != "Admin").Select(u => ToResponse(u)).ToList();
            return Ok(new ApiResponse<List<UserResponse>> { Success = true, Data = filtered });
        }

        [HttpPost("{id}/change-password")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
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
