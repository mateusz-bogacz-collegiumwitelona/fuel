using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.Responses
{
    public class GetProposalStatisticResponse
    {
        public int TotalProposals { get; set; }
        public int ApprovedProposals { get; set; }
        public int RejectedProposals { get; set; }
        public int AcceptedRate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
