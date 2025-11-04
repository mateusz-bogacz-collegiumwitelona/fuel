using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class AddNewPriceProposalRequest
    {

        /// station info
        public string BrandName { get; set; }
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string FuelType { get; set; }

        /// proposal info
        [Required(ErrorMessage = "Photo is required")]
        public IFormFile Photo { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than zero.")]
        public decimal ProposedPrice { get; set; }
    }
}
