using ChatBot_BE.Models;
using ChatBot_BE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;

        public AIController(IAIService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
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

            var result = await _aiService.GetResponse(request);
            return Ok(new { answer = result.Answer, conversationId = result.ConversationId });
        }

        private static string ToShortHash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }
}
