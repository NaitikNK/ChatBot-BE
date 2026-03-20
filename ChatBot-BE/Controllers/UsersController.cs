using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IPolicyService _policyService;

        public UsersController(IUserStore users, AppDbContext db, IPolicyService policyService)
        {
            _users = users;
            _db = db;
            _policyService = policyService;
        }

        /// <summary>
        /// Gets all users currently in the system.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetAll()
        {
            var users = await _users.GetAllAsync();
            var responses = new List<UserResponse>();
            foreach (var user in users)
            {
                responses.Add(await ToResponseAsync(user));
            }

            return Ok(new ApiResponse<List<UserResponse>>
            {
                Success = true,
                Data = responses
            });
        }

        /// <summary>
        /// Gets a paged list of users.
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<PagedResponse<UserResponse>>>> GetList(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Ensure page size is within reasonable bounds
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            pageNumber = Math.Max(1, pageNumber);

            var (users, totalCount) = await _users.GetPagedAsync(pageNumber, pageSize);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = new List<UserResponse>();
            foreach (var user in users)
            {
                items.Add(await ToResponseAsync(user));
            }

            var pagedResponse = new PagedResponse<UserResponse>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return Ok(new ApiResponse<PagedResponse<UserResponse>>
            {
                Success = true,
                Data = pagedResponse
            });
        }

        /// <summary>
        /// Gets a specific user by their ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> Get(int id)
        {
            var user = await _users.GetAsync(id);
            if (user == null) return NotFound(new ApiResponse<UserResponse> { Success = false, Error = "Not found." });
            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Data = await ToResponseAsync(user)
            });
        }

        /// <summary>
        /// Gets a user by their policy number.
        /// </summary>
        [HttpGet("by-policy/{policyNumber}")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> GetByPolicyNumber(string policyNumber)
        {
            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null) return NotFound(new ApiResponse<UserResponse> { Success = false, Error = "Not found." });
            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Data = await ToResponseAsync(user)
            });
        }

        /// <summary>
        /// Creates a new user policy record.
        /// </summary>
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
                    Email = request.Email,
                    PolicyType = request.PolicyType,
                    PolicyName = request.PolicyName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    DateOfBirth = request.DateOfBirth,
                    OwnerSessionId = ""
                });

                return CreatedAtAction(
                    nameof(GetByPolicyNumber),
                    new { policyNumber = created.PolicyNumber },
                    new ApiResponse<UserResponse> { Success = true, Data = await ToResponseAsync(created) });
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

        /// <summary>
        /// Updates an existing user policy record.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UserUpdateRequest request)
        {
            try
            {
                var ok = await _users.UpdateAsync(id, new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PolicyNumber = request.PolicyNumber,
                    Email = request.Email,
                    PolicyType = request.PolicyType,
                    PolicyName = request.PolicyName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    DateOfBirth = request.DateOfBirth,
                    UpdatedAt = DateTime.UtcNow
                });
                return Ok(new ApiResponse<object> { Success = ok });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<object> { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a user by their ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var ok = await _users.DeleteAsync(id);
            return ok
                ? Ok(new ApiResponse<object> { Success = true })
                : NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });
        }

        /// <summary>
        /// Deletes a user by their policy number.
        /// </summary>
        [HttpDelete("by-policy/{policyNumber}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteByPolicyNumber(string policyNumber)
        {
            var ok = await _users.DeleteByPolicyNumberAsync(policyNumber);
            return ok
                ? Ok(new ApiResponse<object> { Success = true })
                : NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });
        }

        private async Task<UserResponse> ToResponseAsync(User user)
        {
            var (policyTypeId, policyTypeName) = await _policyService.ResolvePolicyTypeAsync(user.PolicyType);
            var (policyNameId, policyNameName) = await _policyService.ResolvePolicyNameAsync(user.PolicyName);

            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PolicyNumber = user.PolicyNumber,
                Email = user.Email,
                PolicyTypeId = policyTypeId,
                PolicyType = policyTypeName,
                PolicyNameId = policyNameId,
                PolicyName = policyNameName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                State = user.State,
                PostalCode = user.PostalCode,
                Country = user.Country,
                DateOfBirth = user.DateOfBirth,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
