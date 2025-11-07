using Data.Models;

public class BanRecord
{
    public Guid Id { get; set; }


    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; }

    public string Reason { get; set; }
    public DateTime BannedAt { get; set; }
    public DateTime? BannedUntil { get; set; }
    public bool IsActive { get; set; }

    public Guid AdminId { get; set; }
    public ApplicationUser Admin { get; set; }

    public DateTime? UnbannedAt { get; set; }
    public Guid? UnbannedByAdminId { get; set; }
    public ApplicationUser? UnbannedByAdmin { get; set; }
}