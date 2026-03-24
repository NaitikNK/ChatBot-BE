using ChatBot_BE.Dto;
using ChatBot_BE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        public AIController(IAIService aiService, IInputValidator inputValidator, IConversationContext context)
        {
            _aiService = aiService;
            _inputValidator = inputValidator;
            _context = context;
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

                const string cookieName = "conversationId";

                var conversationId =
                    string.IsNullOrWhiteSpace(request.ConversationId)
                        ? (Request.Cookies.TryGetValue(cookieName, out var fromCookie) ? fromCookie : null)
                        : request.ConversationId;

                if (string.IsNullOrWhiteSpace(conversationId))
                {
                    var fingerprint = $"{HttpContext.Connection.RemoteIpAddress}|{Request.Headers.UserAgent}|{Request.Headers.Origin}";
                    conversationId = ToShortHash(fingerprint);
                }

                request.ConversationId = conversationId;
                Response.Cookies.Append(cookieName, conversationId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                // Read Auth headers
                if (Request.Headers.TryGetValue("X-User-Id", out var userIdVal) && int.TryParse(userIdVal, out var userId))
                {
                    _context.IsAuthenticated = true;
                    _context.UserId = userId;
                }
                _context.ConversationId = conversationId; // Always use the actual session ID
                if (Request.Headers.TryGetValue("X-User-Role", out var roleVal))
                {
                    _context.Role = roleVal.ToString();
                }

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
                // Detailed logging for the 500 error
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = env.IsDevelopment() ? ex.ToString() : "An internal server error occurred while processing your chat request."
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
