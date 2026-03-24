using ChatBot_BE.Dto;
using ChatBot_BE.Data;
using ChatBot_BE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [EnableRateLimiting("fixed")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IInputValidator _inputValidator;
        private readonly IConversationContext _context;
        private readonly AppDbContext _db;

        public AIController(IAIService aiService, IInputValidator inputValidator, IConversationContext context, AppDbContext db)
        {
            _aiService = aiService;
            _inputValidator = inputValidator;
            _context = context;
            _db = db;
        }

        /// <summary>
        /// Sends a message to the AI chatbot and gets a response.
        /// </summary>
        /// <param name="request">The chat request containing the user message and optional conversation ID.</param>
        /// <returns>A chat reply with the AI's response and conversation ID.</returns>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, [FromServices] IWebHostEnvironment env)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = string.Join("; ", errors)
                    });
                }

                // Validate input (profanity check, spell check)
                var validationResult = _inputValidator.Validate(request.Message);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = validationResult.ErrorMessage
                    });
                }

                // Use corrected text if spelling was fixed
                if (!string.IsNullOrEmpty(validationResult.CorrectedText) && 
                    validationResult.CorrectedText != request.Message)
                {
                    request.Message = validationResult.CorrectedText;
                }

                // Read Auth headers FIRST (needed for user-specific conversationId)
                int? userId = null;
                if (Request.Headers.TryGetValue("X-User-Id", out var userIdVal) && int.TryParse(userIdVal, out var parsedUserId))
                {
                    _context.IsAuthenticated = true;
                    _context.UserId = parsedUserId;
                    userId = parsedUserId;

                    // Look up the user's RoleId from the database
                    var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == parsedUserId);
                    if (user != null)
                    {
                        _context.RoleId = user.RoleId;
                    }
                }
                if (Request.Headers.TryGetValue("X-User-Role", out var roleVal))
                {
                    _context.Role = roleVal.ToString();
                }

                // Use provided conversationId for existing chats, or generate a new one for new chats
                var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                    ? Guid.NewGuid().ToString("N")
                    : request.ConversationId.Trim();

                request.ConversationId = conversationId;

                _context.ConversationId = conversationId;

                var result = await _aiService.GetResponse(request);
                return Ok(new ApiResponse<ChatReply> 
                { 
                    Success = true, 
                    Data = new ChatReply 
                    { 
                        Answer = result.Answer, 
                        ConversationId = result.ConversationId 
                    } 
                });
            }
            catch (Exception ex)
            {
                // Temporarily expose full error to find the root cause on Render
                // We will revert this to 'Generic Error' once debugging is complete
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "An internal server error occurred while processing your chat request."
                });
            }
        }

        /// <summary>
        /// Retrieves the default AI greeting message dynamically from the agent.
        /// </summary>
        [HttpGet("greeting")]
        public async Task<IActionResult> GetGreeting()
        {
            var greeting = await _aiService.GetGreetingAsync();
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = greeting
            });
        }

        private static string ToShortHash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }
}
