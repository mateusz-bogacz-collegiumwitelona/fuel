namespace DTO.Responses
{
    public class UserReportsResponse
    {
        public string ReportedUserName { get; set; }
        public string ReportedUserEmail { get; set; }
        public string ReportingUserName { get; set; } 
        public string ReportingUserEmail { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
