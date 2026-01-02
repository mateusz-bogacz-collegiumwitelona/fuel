using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class ChangeReportStatusRequest
    {
        [Required]
        public bool IsAccepted { get; set; }

        [Required]
        [EmailAddress]
        public string ReportedUserEmail { get; set; }

        [Required]
        [EmailAddress]
        public string ReportingUserEmail { get; set; }

        [Required]
        public DateTime ReportCreatedAt { get; set; }

        public string? Reason { get; set; }

        public int? Days { get; set; }
    }
}
