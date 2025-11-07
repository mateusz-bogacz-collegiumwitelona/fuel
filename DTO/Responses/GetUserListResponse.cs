namespace DTO.Responses
{
    public class GetUserListResponse
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Roles { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
