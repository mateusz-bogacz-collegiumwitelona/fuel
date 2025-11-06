using Data.Enums;
using System.ComponentModel.DataAnnotations;

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

        [MaxLength(64)]
        public string PhotoToken { get; set; }

        public string? AdminComment { get; set; }
        public Guid? ReviewedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        
    }
}

