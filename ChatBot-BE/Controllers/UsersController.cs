using ChatBot_BE.Data;
using ChatBot_BE.Services;
using ChatBot_BE.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserStore _users;

        public UsersController(IUserStore users)
        {
            _users = users;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetAll()
        {
            var users = await _users.GetAllAsync();
            return Ok(new ApiResponse<List<UserResponse>>
            {
                Success = true,
                Data = users.Select(ToResponse).ToList()
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> Get(int id)
        {
            var user = await _users.GetAsync(id);
            if (user == null) return NotFound(new ApiResponse<UserResponse> { Success = false, Error = "Not found." });
            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Data = ToResponse(user)
            });
        }

        [HttpGet("by-policy/{policyNumber}")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> GetByPolicyNumber(string policyNumber)
        {
            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null) return NotFound(new ApiResponse<UserResponse> { Success = false, Error = "Not found." });
            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Data = ToResponse(user)
            });
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
                    PolicyNumber = request.PolicyNumber,
                    Email = request.Email
                });

                return CreatedAtAction(
                    nameof(GetByPolicyNumber),
                    new { policyNumber = created.PolicyNumber },
                    new ApiResponse<UserResponse> { Success = true, Data = ToResponse(created) });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<UserResponse> { Success = false, Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<UserResponse> { Success = false, Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] UserUpdateRequest request)
        {
            try
            {
                var ok = await _users.UpdateAsync(id, new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PolicyNumber = request.PolicyNumber,
                    Email = request.Email
                });
                return ok ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var ok = await _users.DeleteAsync(id);
            return ok
                ? Ok(new ApiResponse<object> { Success = true })
                : NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });
        }

        [HttpDelete("by-policy/{policyNumber}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteByPolicyNumber(string policyNumber)
        {
            var ok = await _users.DeleteByPolicyNumberAsync(policyNumber);
            return ok
                ? Ok(new ApiResponse<object> { Success = true })
                : NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });
        }

        private static UserResponse ToResponse(User user)
        {
            return new UserResponse
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PolicyNumber = user.PolicyNumber,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
