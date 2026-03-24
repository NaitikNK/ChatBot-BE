using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatBot_BE.Services;
using ChatBot_BE.Dto;
using ChatBot_BE.Model;
using ChatBot_BE.Data;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/policies")]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyStore _policyStore;
        private readonly IPolicyService _policyService;
        private readonly AppDbContext _db;

        public PolicyController(IPolicyStore policyStore, IPolicyService policyService, AppDbContext db)
        {
            _policyStore = policyStore;
            _policyService = policyService;
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

        [HttpGet("generate-policy-number")]
        public ActionResult<ApiResponse<string>> GeneratePolicyNumber()
        {
            var number = "POL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return Ok(new ApiResponse<string> { Success = true, Data = number });
        }

        [HttpGet("types")]
        public async Task<ActionResult<ApiResponse<List<DropdownOptionDto>>>> GetPolicyTypes()
        {
            var types = await _policyService.GetPolicyTypesAsync();
            return Ok(new ApiResponse<List<DropdownOptionDto>> { Success = true, Data = types });
        }

        [HttpGet("names")]
        public async Task<ActionResult<ApiResponse<List<DropdownOptionDto>>>> GetPolicyNames([FromQuery] string? typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                return Ok(new ApiResponse<List<DropdownOptionDto>> { Success = true, Data = new List<DropdownOptionDto>() });
            }
            var names = await _policyService.GetPolicyNamesByTypeAsync(typeId);
            return Ok(new ApiResponse<List<DropdownOptionDto>> { Success = true, Data = names });
        }

        /// <summary>
        /// Admin → all policies. User → only their own policies.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PolicyResponse>>>> GetAll()
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<List<PolicyResponse>> { Success = false, Error = "User context missing. Send X-User-Id header." });

            var policies = isAdmin
                ? await _policyStore.GetAllAsync()
                : await _policyStore.GetAllByUserAsync(currentUser.Id);

            var responses = new List<PolicyResponse>();
            foreach (var p in policies)
            {
                responses.Add(await ToResponseAsync(p));
            }

            return Ok(new ApiResponse<List<PolicyResponse>> { Success = true, Data = responses });
        }

        /// <summary>
        /// Admin → all policies (paged). User → only their own policies (paged).
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PolicyResponse>>>> GetList(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<PagedResponse<PolicyResponse>> { Success = false, Error = "User context missing. Send X-User-Id header." });

            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            pageNumber = Math.Max(1, pageNumber);

            var (policies, totalCount) = isAdmin
                ? await _policyStore.GetPagedAsync(pageNumber, pageSize)
                : await _policyStore.GetPagedByUserAsync(currentUser.Id, pageNumber, pageSize);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = new List<PolicyResponse>();
            foreach (var p in policies)
            {
                items.Add(await ToResponseAsync(p));
            }

            return Ok(new ApiResponse<PagedResponse<PolicyResponse>>
            {
                Success = true,
                Data = new PagedResponse<PolicyResponse>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                }
            });
        }

        /// <summary>
        /// Admin → any policy. User → only if policy.UserId matches.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PolicyResponse>>> Get(int id)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<PolicyResponse> { Success = false, Error = "User context missing. Send X-User-Id header." });

            var policy = await _policyStore.GetAsync(id);
            if (policy == null)
                return NotFound(new ApiResponse<PolicyResponse> { Success = false, Error = "Not found." });

            if (!isAdmin && policy.UserId != currentUser.Id)
                return StatusCode(403, new ApiResponse<PolicyResponse> { Success = false, Error = "You do not have permission to access this policy." });

            return Ok(new ApiResponse<PolicyResponse> { Success = true, Data = await ToResponseAsync(policy) });
        }

        /// <summary>
        /// Creates a new policy. Regular users can only create for themselves; admin can set any UserId.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PolicyResponse>>> Create([FromBody] PolicyCreateRequest request)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<PolicyResponse> { Success = false, Error = "User context missing. Send X-User-Id header." });

            // Regular users can only create policies for themselves
            if (!isAdmin)
            {
                request.UserId = currentUser.Id;
            }

            try
            {
                var created = await _policyStore.AddAsync(new Policy
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
                    OwnerSessionId = "",
                    UserId = request.UserId
                });

                return CreatedAtAction(nameof(Get), new { id = created.Id }, new ApiResponse<PolicyResponse> { Success = true, Data = await ToResponseAsync(created) });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<PolicyResponse> { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Admin → any policy. User → only if policy.UserId matches.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] PolicyUpdateRequest request)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<object> { Success = false, Error = "User context missing. Send X-User-Id header." });

            var existing = await _policyStore.GetAsync(id);
            if (existing == null)
                return NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });

            if (!isAdmin && existing.UserId != currentUser.Id)
                return StatusCode(403, new ApiResponse<object> { Success = false, Error = "You do not have permission to update this policy." });

            if (existing.OwnerSessionId == "SEEDED_RECORD" && !isAdmin)
                return BadRequest(new ApiResponse<object> { Success = false, Error = "Cannot modify seeded records." });

            try
            {
                var ok = await _policyStore.UpdateAsync(id, new Policy
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
                    DateOfBirth = request.DateOfBirth
                });
                return Ok(new ApiResponse<object> { Success = ok });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Admin → any policy. User → only if policy.UserId matches.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var (currentUser, isAdmin) = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized(new ApiResponse<object> { Success = false, Error = "User context missing. Send X-User-Id header." });

            var existing = await _policyStore.GetAsync(id);
            if (existing == null)
                return NotFound(new ApiResponse<object> { Success = false, Error = "Not found." });

            if (!isAdmin && existing.UserId != currentUser.Id)
                return StatusCode(403, new ApiResponse<object> { Success = false, Error = "You do not have permission to delete this policy." });

            if (existing.OwnerSessionId == "SEEDED_RECORD" && !isAdmin)
                return BadRequest(new ApiResponse<object> { Success = false, Error = "Cannot delete seeded records." });

            var ok = await _policyStore.DeleteAsync(id);
            return Ok(new ApiResponse<object> { Success = ok });
        }

        private async Task<PolicyResponse> ToResponseAsync(Policy p)
        {
            var (policyTypeId, policyTypeName) = await _policyService.ResolvePolicyTypeAsync(p.PolicyType);
            var (policyNameId, policyNameName) = await _policyService.ResolvePolicyNameAsync(p.PolicyName);

            return new PolicyResponse
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PolicyNumber = p.PolicyNumber,
                Email = p.Email,
                PolicyTypeId = policyTypeId,
                PolicyType = policyTypeName,
                PolicyNameId = policyNameId,
                PolicyName = policyNameName,
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                City = p.City,
                State = p.State,
                PostalCode = p.PostalCode,
                Country = p.Country,
                DateOfBirth = p.DateOfBirth,
                IsDefault = p.OwnerSessionId == "SEEDED_RECORD",
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UserId = p.UserId
            };
        }
    }
}
