using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.Responses
{
    public class FindFuelTypeRequest
    {
        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
    }
}
