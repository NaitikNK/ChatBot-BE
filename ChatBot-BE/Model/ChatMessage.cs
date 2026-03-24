using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatBot_BE.Model
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ConversationId { get; set; } = string.Empty;

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        /// <summary>
        /// The user's RoleId (e.g., 1=Admin, 2=User) at the time of the message.
        /// </summary>
        public int? RoleId { get; set; }

        /// <summary>
        /// The message author type: System, User, or Assistant.
        /// </summary>
        [Required]
        public string AuthorType { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
