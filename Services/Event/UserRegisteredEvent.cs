using Data.Models;
using Services.Event.Interfaces;

namespace Services.Event
{
    public class UserRegisteredEvent : IEvent
    {
        public ApplicationUser User { get; }
        public string ConfirmationToken { get; }

        public UserRegisteredEvent(ApplicationUser user, string token)
        {
            User = user;
            ConfirmationToken = token;
        }
    }
}
