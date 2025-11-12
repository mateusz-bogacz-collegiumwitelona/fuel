namespace DTO.Responses
{
    public class GetUserInfoResponse
    {
        public string UserName { get; set; }
        public string Email { get; set; }

        public GetProposalStatisticResponse ProposalStatistics { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
