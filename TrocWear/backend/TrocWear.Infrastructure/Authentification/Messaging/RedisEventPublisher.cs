using System.Text.Json;
using StackExchange.Redis;
using TrocWear.BuildingBlocks.EventContracts;
using TrocWear.BuildingBlocks.Messaging;
namespace AuthService.Infrastructure.Messaging;
public sealed class RedisEventPublisher(IConnectionMultiplexer redis) : IEventPublisher
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public async Task PublishAsync(EventEnvelope message, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await redis.GetDatabase().ListRightPushAsync(QueueNames.Published, JsonSerializer.Serialize(message, Options));
    }
}
