using EasyNetQ;

namespace MessagingService.Messaging;

/// <summary>
/// EasyNetQ-backed implementation of <see cref="IMessageClient"/>.
/// </summary>
public class MessageClient : IMessageClient
{
    private readonly IBus _bus;

    public MessageClient(IBus bus) => _bus = bus;

    public Task PublishAsync<T>(T message) where T : class
        => _bus.PubSub.PublishAsync(message);

    public Task SubscribeAsync<T>(string subscriptionId, Func<T, Task> handler) where T : class
        => _bus.PubSub.SubscribeAsync(subscriptionId, handler);
}
