using ChatBot_BE.Dto;
using ChatBot_BE.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/policies")]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PolicyController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        /// <summary>
        /// Gets all available policy types (e.g., Personal, Vehicle, Medical).
        /// </summary>
        [HttpGet("types")]
        public async Task<ActionResult<ApiResponse<List<DropdownOptionDto>>>> GetPolicyTypes()
        {
            var types = await _policyService.GetPolicyTypesAsync();

            return Ok(new ApiResponse<List<DropdownOptionDto>>
            {
                Success = true,
                Data = types
            });
        }

        /// <summary>
        /// Gets all policy names associated with a specific policy type.
        /// </summary>
        /// <param name="typeId">The ID or name of the policy type.</param>
        /// <param name="type">Alias for typeId.</param>
        [HttpGet("names")]
        public async Task<ActionResult<ApiResponse<List<DropdownOptionDto>>>> GetPolicyNames([FromQuery] string? typeId, [FromQuery] string? type)
        {
            var efficientTypeId = typeId ?? type;

            if (string.IsNullOrEmpty(efficientTypeId))
            {
                return BadRequest(new ApiResponse<List<DropdownOptionDto>>
                {
                    Success = false,
                    Error = "Policy type ID (typeId or type) is required."
                });
            }

            var names = await _policyService.GetPolicyNamesByTypeAsync(efficientTypeId);

            return Ok(new ApiResponse<List<DropdownOptionDto>>
            {
                Success = true,
                Data = names
            });
        }
    }
}
