using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class SetLockoutForUserRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Range(1, 3650, ErrorMessage = "Days must be between 1 and 3650 (10 years)")]
        public int? Days { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 500 characters")]
        public string Reason { get; set; }
    }
}
