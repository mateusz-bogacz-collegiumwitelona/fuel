using Data.Enums;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class PriceProposal
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }
        
        public Guid StationId { get; set; }
        public Station Station { get; set; }

        public Guid FuelTypeId { get; set; }
        public FuelType FuelType { get; set; }

        public decimal ProposedPrice { get; set; }
        public string PhotoUrl { get; set; }
        public PriceProposalStatus Status { get; set; }
        public string? AdminComment { get; set; }
        public Guid RewiewedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        
    }
}

