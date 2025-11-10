namespace DTO.Responses
{
    public class LoginResponse
    {
        public string Message { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public List<string> Roles { get; set; }
    }
}
