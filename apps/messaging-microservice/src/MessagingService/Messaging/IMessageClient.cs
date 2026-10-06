namespace MessagingService.Messaging;

/// <summary>
/// Thin publish/subscribe abstraction over the message broker. The rest of the
/// service depends on this instead of EasyNetQ directly, so the broker can be
/// swapped without touching the domain code.
/// </summary>
public interface IMessageClient
{
    Task PublishAsync<T>(T message) where T : class;
    Task SubscribeAsync<T>(string subscriptionId, Func<T, Task> handler) where T : class;
}
