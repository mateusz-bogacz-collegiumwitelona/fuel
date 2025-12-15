using Data.Models;
using Services.Event.Interfaces;

namespace Services.Event
{
    public class UserUnlockedEvent : IEvent
    {
        public ApplicationUser User { get; }
        public string AdminName { get; }

        public UserUnlockedEvent(ApplicationUser user, ApplicationUser admin)
        {
            User = user;
            AdminName = admin.UserName;
        }
    }
}
