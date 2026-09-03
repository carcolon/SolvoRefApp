namespace Core.Feature.Login.LoginUser
{
    public class LoginUserResponseDto
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
        public string Token { get; set; } = string.Empty;

    }
}