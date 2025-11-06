namespace DTO.Responses
{
    public class GetProposalStatisticResponse
    {
        public int TotalProposals { get; set; }
        public int ApprovedProposals { get; set; }
        public int RejectedProposals { get; set; }
        public int AcceptedRate { get; set; }
        public int Points { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
