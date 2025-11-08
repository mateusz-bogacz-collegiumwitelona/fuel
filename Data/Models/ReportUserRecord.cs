using Data.Enums;

namespace Data.Models
{
    public class ReportUserRecord
    {
        public Guid Id { get; set; }

        public Guid ReportingUserId { get; set; }
        public ApplicationUser ReportingUser { get; set; }
        
        public Guid ReportedUserId { get; set; }
        public ApplicationUser ReportedUser { get; set; }

        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;
        
        public Guid? ReviewedByAdminId { get; set; }
        public ApplicationUser? ReviewedByAdmin { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
