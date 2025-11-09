using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class ChangePriceProposalStatusRequest
    {
        [Required(ErrorMessage = "IsAccepted is required")]
        public bool IsAccepted { get; set; }

        [Required(ErrorMessage = "PhotoToken is required")]
        [StringLength(100, MinimumLength = 1)]
        public string PhotoToken { get; set; }

        [Required(ErrorMessage = "UserEmail is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "NewPrice is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than zero.")]
        public decimal NewPrice { get; set; }
    }
}
