using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class DeleteAccountRequest
    {
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
