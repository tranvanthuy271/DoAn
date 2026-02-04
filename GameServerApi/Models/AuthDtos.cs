namespace GameServerApi.Models
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
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

