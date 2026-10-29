namespace AuthService.Application.Interface.IRepositories;
public interface ISessionRepository
{
    Task SetAsync(string sessionHash, Guid userId, TimeSpan ttl, CancellationToken ct);
    Task<Guid?> GetUserIdAsync(string sessionHash, CancellationToken ct);
    Task DeleteAsync(string sessionHash, CancellationToken ct);
}
