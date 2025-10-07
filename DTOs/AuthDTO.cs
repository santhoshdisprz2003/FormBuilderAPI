using System;
using System.ComponentModel.DataAnnotations;

namespace FormBuilderAPI.DTOs
{
    // Used for login/register requests
    public class AuthDTO
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        // Optional: for admin to assign role on registration
        public string Role { get; set; } = "Learner";
    }

    // Returned after successful login/register
    public class AuthResponseDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
