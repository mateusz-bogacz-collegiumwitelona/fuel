using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class ProposalStatisict
    {
        public Guid Id { get; set; }

        public Guid UserId{ get; set; }
        public ApplicationUser User { get; set; }

        public decimal ProposedPrice { get; set; }
        public int TotalProposals { get; set; }
        public int ApprovedProposals { get; set; }
        public int RejectedProposals { get; set; }
        public int AcceptedRate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
