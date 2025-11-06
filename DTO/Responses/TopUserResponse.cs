namespace DTO.Responses
{
    public class TopUserResponse
    {
        public string UserName { get; set; }
        public int? TotalProposals { get; set; }
        public int? ApprovedProposals { get; set; }
        public int? RejectedProposals { get; set; }
        public int? AcceptedRate { get; set; }
        public int? Points { get; set; }
    }
}
