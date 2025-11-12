using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class FindStationPriceProposalRequest
    {
        [Required(ErrorMessage = "Brand name is required")]
        public string BrandName { get; set; }

        [Required(ErrorMessage = "Street is required")]
        public string Street { get; set; }

        [Required(ErrorMessage = "House number is required")]
        public string HouseNumber { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int? PageNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page size must be greater than 0")]
        public int? PageSize { get; set; }
    }
}
