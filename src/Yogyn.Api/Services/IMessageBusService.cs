using Yogyn.Api.Events;

namespace Yogyn.Api.Services;

public interface IMessageBusService
{
    Task PublishAsync<T>(T eventMessage) where T : IEvent;
}
