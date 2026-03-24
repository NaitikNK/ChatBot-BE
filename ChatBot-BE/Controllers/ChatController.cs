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

        /// <summary>
        /// Returns the current user's conversation session IDs only.
        /// </summary>
        [HttpGet("sessions")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetSessions()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdVal) && int.TryParse(userIdVal, out var userId))
            {
                var sessions = await _sessions.GetSessionsForUserAsync(userId);
                return Ok(new ApiResponse<List<string>> { Success = true, Data = sessions });
            }

            return BadRequest(new ApiResponse<List<string>> { Success = false, Error = "User context missing." });
        }

        /// <summary>
        /// Returns history for a conversation, but only if it belongs to the current user.
        /// </summary>
        [HttpGet("history/{conversationId}")]
        public async Task<ActionResult<ApiResponse<List<ChatMessageResponse>>>> GetHistory(string conversationId)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdVal) || !int.TryParse(userIdVal, out var userId))
            {
                return BadRequest(new ApiResponse<List<ChatMessageResponse>> { Success = false, Error = "User context missing." });
            }

            // Verify the conversation belongs to this user
            var userSessions = await _sessions.GetSessionsForUserAsync(userId);
            if (!userSessions.Contains(conversationId))
            {
                return StatusCode(403, new ApiResponse<List<ChatMessageResponse>> { Success = false, Error = "You do not have access to this conversation." });
            }

            var history = await _sessions.GetHistoryAsync(conversationId);
            var response = history.Select(m => new ChatMessageResponse
            {
                AuthorType = m.AuthorType,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
 
            return Ok(new ApiResponse<List<ChatMessageResponse>> { Success = true, Data = response });
        }

        /// <summary>
        /// Deletes a conversation's history, but only if it belongs to the current user.
        /// </summary>
        [HttpDelete("history/{conversationId}")]
        public async Task<IActionResult> DeleteHistory(string conversationId)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdVal) || !int.TryParse(userIdVal, out var userId))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Error = "User context missing." });
            }

            // Verify the conversation belongs to this user
            var userSessions = await _sessions.GetSessionsForUserAsync(userId);
            if (!userSessions.Contains(conversationId))
            {
                return StatusCode(403, new ApiResponse<object> { Success = false, Error = "You do not have access to this conversation." });
            }

            await _sessions.DeleteHistoryAsync(conversationId);
            return Ok(new ApiResponse<object> { Success = true });
        }

        /// <summary>
        /// Deletes all conversation history for the current user only.
        /// </summary>
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
        public string AuthorType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
