namespace Data.Models
{
    public class ProposalStatistic
    {
        public Guid Id { get; set; }

        public Guid UserId{ get; set; }
        public ApplicationUser User { get; set; }
        public int? TotalProposals { get; set; }
        public int? ApprovedProposals { get; set; }
        public int? RejectedProposals { get; set; }
        public int? AcceptedRate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
