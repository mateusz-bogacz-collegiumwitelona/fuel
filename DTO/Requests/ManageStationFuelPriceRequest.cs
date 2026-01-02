using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class ManageStationFuelPriceRequest
    {
        public FindStationRequest Station { get; set; }
       
        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; }
        
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than zero.")]
        public decimal Price { get; set; }
    }
}
