using ChatBot_BE.Services;
using ChatBot_BE.Dto;
using ChatBot_BE.Model;
using ChatBot_BE.Data;
using Microsoft.AspNetCore.Mvc;

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
            return Ok(new ApiResponse<List<UserResponse>>
            {
                Success = true,
                Data = users.Select(u => ToResponse(u)).ToList()
            });
        }

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

            var pagedResponse = new PagedResponse<UserResponse>
            {
                Items = users.Select(u => ToResponse(u)).ToList(),
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

        private UserResponse ToResponse(User user)
        {
            // Look up PolicyType
            int? policyTypeId = null;
            string policyTypeName = user.PolicyType;

            if (int.TryParse(user.PolicyType, out int typeId))
            {
                var typeMaster = _db.PolicyTypes.Find(typeId);
                if (typeMaster != null)
                {
                    policyTypeId = typeMaster.Id;
                    policyTypeName = typeMaster.Name;
                }
            }
            else if (!string.IsNullOrEmpty(user.PolicyType))
            {
                var typeMaster = _db.PolicyTypes.FirstOrDefault(t => t.Name == user.PolicyType);
                if (typeMaster != null)
                {
                    policyTypeId = typeMaster.Id;
                    policyTypeName = typeMaster.Name;
                }
            }

            // Look up PolicyName
            int? policyNameId = null;
            string? policyNameName = user.PolicyName;

            if (int.TryParse(user.PolicyName, out int nameId))
            {
                var nameMaster = _db.PolicyNames.Find(nameId);
                if (nameMaster != null)
                {
                    policyNameId = nameMaster.Id;
                    policyNameName = nameMaster.Name;
                }
            }
            else if (!string.IsNullOrEmpty(user.PolicyName))
            {
                var nameMaster = _db.PolicyNames.FirstOrDefault(n => n.Name == user.PolicyName);
                if (nameMaster != null)
                {
                    policyNameId = nameMaster.Id;
                    policyNameName = nameMaster.Name;
                }
            }

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
