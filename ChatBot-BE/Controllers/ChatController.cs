using ChatBot_BE.Dto;
using ChatBot_BE.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatSessionStore _sessions;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatSessionStore sessions, ILogger<ChatController> logger)
        {
            _sessions = sessions;
            _logger = logger;
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetSessions()
        {
            // Extract User ID from headers (simulated auth for this project)
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdVal) && int.TryParse(userIdVal, out var userId))
            {
                var sessions = await _sessions.GetSessionsForUserAsync(userId);
                return Ok(new ApiResponse<List<string>> { Success = true, Data = sessions });
            }

            return BadRequest(new ApiResponse<List<string>> { Success = false, Error = "User context missing." });
        }

        [HttpGet("history/{conversationId}")]
        public async Task<ActionResult<ApiResponse<List<ChatMessageResponse>>>> GetHistory(string conversationId)
        {
            var history = await _sessions.GetHistoryAsync(conversationId);
            var response = history.Select(m => new ChatMessageResponse
            {
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
 
            return Ok(new ApiResponse<List<ChatMessageResponse>> { Success = true, Data = response });
        }

        [HttpDelete("history/{conversationId}")]
        public async Task<IActionResult> DeleteHistory(string conversationId)
        {
            await _sessions.DeleteHistoryAsync(conversationId);
            return Ok(new ApiResponse<object> { Success = true });
        }

        [HttpDelete("history/all")]
        public async Task<IActionResult> DeleteAllHistory()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdVal) && int.TryParse(userIdVal, out var userId))
            {
                await _sessions.DeleteAllHistoryAsync(userId);
                return Ok(new ApiResponse<object> { Success = true });
            }

            return BadRequest(new ApiResponse<object> { Success = false, Error = "User context missing." });
        }
    }

    public class ChatMessageResponse
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
