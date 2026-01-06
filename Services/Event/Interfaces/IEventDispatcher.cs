namespace Services.Event.Interfaces
{
    public interface IEventDispatcher
    {
        Task PublishAsync<T>(T @event) where T : IEvent;
    }
}
