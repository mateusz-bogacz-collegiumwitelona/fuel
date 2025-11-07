using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletdAt { get; set; }

        public ICollection<PriceProposal> PriceProposal { get; set; } = new List<PriceProposal>();
        public ProposalStatistic ProposalStatistic { get; set; }

        public ICollection<BanRecord> BanRecords { get; set; }

        public ICollection<BanRecord> BansReceived { get; set; }

        public ICollection<BanRecord> BansGiven { get; set; }

        public ICollection<BanRecord> UnbansGiven { get; set; }
    }
}
