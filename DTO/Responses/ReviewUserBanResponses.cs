namespace DTO.Responses
{
    public class ReviewUserBanResponses
    {
        public string UserName { get; set; }
        public string Reason { get; set; }

        public DateTime BannedAt { get; set; }
        public DateTime? BannedUntil { get; set; }
        public string BannedBy { get; set; }
    }
}
