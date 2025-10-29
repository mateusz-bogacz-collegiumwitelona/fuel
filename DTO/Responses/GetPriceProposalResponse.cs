using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.Responses
{
    public class GetPriceProposalResponse
    {
        public string Email { get; set; } 
        public string BrandName { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; } 
        public string PostalCode { get; set; } 
        public string FuelType { get; set; }
        public decimal ProposedPrice { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
