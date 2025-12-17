using Services.Event.Interfaces;
using Services.Helpers;

namespace Services.Event.Handlers
{
    public class InvalidateBannedUserCacheHandler : IEventHandler<UserBannedEvent>
    {
        private readonly CacheService _cache;

        public InvalidateBannedUserCacheHandler(CacheService cache)
        {
            _cache = cache;
        }

        public async Task HandleAsync(UserBannedEvent @event)
        {
            await _cache.RemoveByPatternAsync($"{CacheService.CacheKeys.UsersList}*");
            await _cache.InvalidateUserInfoCacheAsync(@event.User.Email);
        }
    }
}
