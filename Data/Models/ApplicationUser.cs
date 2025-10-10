using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
