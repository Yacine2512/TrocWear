using StackExchange.Redis;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
namespace AuthService.Infrastructure.Repositories;
public sealed class RedisSessionRepository(IConnectionMultiplexer redis) : ISessionRepository
{
    public const string Prefix = "session:";
    public async Task SetAsync(string sessionHash, Guid userId, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!await redis.GetDatabase().StringSetAsync(Prefix + sessionHash, userId.ToString("D"), ttl))
            throw new InvalidOperationException("Impossible de créer la session Redis.");
    }
    public async Task<Guid?> GetUserIdAsync(string sessionHash, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var stored = await redis.GetDatabase().StringGetAsync(Prefix + sessionHash);
        return stored.HasValue && Guid.TryParse(stored.ToString(), out var id) ? id : null;
    }
    public async Task DeleteAsync(string sessionHash, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await redis.GetDatabase().KeyDeleteAsync(Prefix + sessionHash);
    }
}
