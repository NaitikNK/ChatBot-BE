using System;
using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Model
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string RoleName { get; set; } = string.Empty;
    }
}
