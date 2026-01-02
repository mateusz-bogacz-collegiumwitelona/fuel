using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class ReportRequest
    {
        [Required(ErrorMessage = "UserName is required")]
        public string ReportedUserName { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(1000, ErrorMessage = "Reason must be beetwen 50 and 1000 characters", MinimumLength = 50)]
        public string Reason { get; set; }
    }
}
