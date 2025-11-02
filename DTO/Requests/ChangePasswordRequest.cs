using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least 6 characters long", MinimumLength = 6)]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?""{}|<>]).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character"
        )]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }
}
