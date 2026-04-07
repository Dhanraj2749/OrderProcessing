using EventBus.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EventBus;

/// <summary>
/// In-memory event bus for local development.
/// Production swap: Azure Service Bus / RabbitMQ.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IntegrationEvent;
    void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent;
}

public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<string, List<Func<IntegrationEvent, Task>>> _handlers = new();
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        => _logger = logger;

    public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
    {
        var key = typeof(T).Name;
        _logger.LogInformation("[EventBus] Publishing {EventType} | Id: {Id}", key, @event.Id);

        if (_handlers.TryGetValue(key, out var handlers))
        {
            var tasks = handlers.Select(h => h(@event));
            await Task.WhenAll(tasks);
        }
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent
    {
        var key = typeof(T).Name;
        _handlers.GetOrAdd(key, _ => new List<Func<IntegrationEvent, Task>>())
                 .Add(e => handler((T)e));
        _logger.LogInformation("[EventBus] Subscribed to {EventType}", key);
    }
}
