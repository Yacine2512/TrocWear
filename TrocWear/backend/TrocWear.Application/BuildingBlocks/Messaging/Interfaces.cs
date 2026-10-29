using TrocWear.BuildingBlocks.EventContracts;
namespace TrocWear.BuildingBlocks.Messaging;
public interface IEventPublisher { Task PublishAsync(EventEnvelope message, CancellationToken ct = default); }
public interface IEventConsumer { Task<EventEnvelope?> ReceiveAsync(CancellationToken ct = default); }
public interface IEventRouter { Task RouteAsync(EventEnvelope message, CancellationToken ct = default); }
public interface IEventHandler<in TEvent> { Task HandleAsync(TEvent evt, CancellationToken ct = default); }
public static class QueueNames
{
    public const string Published = "trocwear:events:published";
    public const string RouterProcessing = "trocwear:router:processing";
    public static string Ready(string service) => $"trocwear:events:{service}:ready";
    public static string Processing(string service) => $"trocwear:events:{service}:processing";
}
