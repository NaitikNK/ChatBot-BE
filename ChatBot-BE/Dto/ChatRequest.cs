using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Dto
{
    public class ChatRequest
    {
        public string? ConversationId { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters.")]
        public string Message { get; set; } = string.Empty;
    }
}
