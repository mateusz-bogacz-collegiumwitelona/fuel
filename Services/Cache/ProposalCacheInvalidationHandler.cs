using Services.Event;
using Services.Event.Interfaces;

namespace Services.Cache
{
    public class ProposalCacheInvalidationHandler : IEventHandler<PriceProposalEvaluatedEvent>
    {
        private readonly CacheService _cache;

        public ProposalCacheInvalidationHandler(CacheService cache)
        {
            _cache = cache;
        }

        public async Task HandleAsync(PriceProposalEvaluatedEvent @event)
        {
            await _cache.InvalidateUserStatsCacheAsync(@event.User.Email);
            if (@event.IsAccepted)
            {
                await _cache.InvalidateStationCacheAsync();
            }
        }
    }
}
