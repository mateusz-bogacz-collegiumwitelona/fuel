using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class ReportRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string ReportedEmail { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(1000, ErrorMessage = "Password must be beetwen 50 and 1000 characters", MinimumLength = 50)]
        public string Reason { get; set; }
    }
}
