using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class AddFuelTypeRequest
    {
        [Required(ErrorMessage = "Name name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Code name is required")]
        public string Code { get; set; }
    }
}
