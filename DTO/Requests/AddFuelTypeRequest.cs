using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class AddFuelTypeRequest
    {
        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
    }
}
