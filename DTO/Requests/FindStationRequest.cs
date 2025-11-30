using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class FindStationRequest
    {
        [Required(ErrorMessage = "Brand name is required")]
        public string BrandName { get; set; }

        [Required(ErrorMessage = "Street is required")]
        public string Street { get; set; }

        [Required(ErrorMessage = "House number is required")]
        public string HouseNumber { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }
    }
}
