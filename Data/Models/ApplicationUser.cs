using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public int Points { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<PriceProposal> PriceProposal { get; set; } = new List<PriceProposal>();
        public ProposalStatistic ProposalStatistic { get; set; }
    }
}
