using Services.Event.Interfaces;
using Services.Helpers;

namespace Services.Event.Handlers
{
    public class InvalidateUnlockedUserCacheHandler : IEventHandler<UserUnlockedEvent>
    {
        private readonly CacheService _cache;
        
        public InvalidateUnlockedUserCacheHandler(CacheService cache)
        {
            _cache = cache;
        }

        public async Task HandleAsync(UserUnlockedEvent @event)
        {
            await _cache.RemoveByPatternAsync($"{CacheService.CacheKeys.UsersList}*");
            await _cache.InvalidateUserInfoCacheAsync(@event.User.Email);
        }
    }
}
