namespace AuthService.Application.Interface.IService;
public interface ISessionService
{
    Task<string> CreateAsync(Guid userId, CancellationToken ct);
    Task<Guid?> ResolveAsync(string opaqueToken, CancellationToken ct);
    Task RevokeAsync(string opaqueToken, CancellationToken ct);
}
