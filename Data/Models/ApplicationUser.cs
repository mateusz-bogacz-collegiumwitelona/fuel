using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<PriceProposal> PriceProposal { get; set; } = new List<PriceProposal>();
        public ProposalStatistic ProposalStatistic { get; set; }
    }
}
