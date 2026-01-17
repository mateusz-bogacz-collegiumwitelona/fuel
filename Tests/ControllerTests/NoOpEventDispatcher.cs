using Services.Event.Interfaces;

namespace Tests.ControllerTests
{
    internal class NoOpEventDispatcher : IEventDispatcher
    {
        public Task PublishAsync<T>(T @event) where T : IEvent
        {
            return Task.CompletedTask;
        }
    }
}