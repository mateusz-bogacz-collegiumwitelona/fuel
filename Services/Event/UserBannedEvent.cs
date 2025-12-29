using Data.Models;
using Services.Event.Interfaces;

namespace Services.Event
{
    public class UserBannedEvent : IEvent
    {
        public ApplicationUser User { get; }
        public Guid AdminId { get; }
        public string AdminName { get; } 
        public string Reason { get; }
        public int? Days { get; }

        public UserBannedEvent(ApplicationUser user, ApplicationUser admin, string reason, int? days)
        {
            User = user;
            AdminId = admin.Id;
            AdminName = admin.UserName;
            Reason = reason;
            Days = days;
        }
    }
}
