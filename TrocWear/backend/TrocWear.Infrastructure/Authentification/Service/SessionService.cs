using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
namespace AuthService.Infrastructure.Service;
public sealed class SessionService(ISessionRepository repository) : ISessionService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(3600);
    public async Task<string> CreateAsync(Guid userId, CancellationToken ct)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+','-').Replace('/','_');
        await repository.SetAsync(Hash(token), userId, Lifetime, ct);
        return token; // transmis seulement au cookie HTTP-only
    }
    public Task<Guid?> ResolveAsync(string opaqueToken, CancellationToken ct) =>
        repository.GetUserIdAsync(Hash(opaqueToken), ct);
    public Task RevokeAsync(string opaqueToken, CancellationToken ct) =>
        repository.DeleteAsync(Hash(opaqueToken), ct);
    public static string Hash(string token) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
