using System.ComponentModel.DataAnnotations;

namespace GameServerApi.Models
{
    public class LoginRequest
    {
        [Required]
        [MinLength(3), MaxLength(30)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6), MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required]
        [MinLength(3), MaxLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9_]+$",
            ErrorMessage = "Username chỉ được chứa chữ cái, chữ số và dấu gạch dưới.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6), MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int User_Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class RegisterResponse
    {
        public string Token { get; set; } = string.Empty;
        public int User_Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

