using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Event.Interfaces;

namespace Services.Event
{
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public EventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task PublishAsync<T>(T @event) where T : IEvent
        {
            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IEventHandler<T>>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(@event);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventDispatcher>>();
                    logger.LogError(ex, "Error while handling event {EventName}", typeof(T).Name);
                }
            }
        }
    }
}
