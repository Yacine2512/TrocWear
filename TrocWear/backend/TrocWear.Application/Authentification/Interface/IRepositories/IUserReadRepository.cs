using AuthService.Domain.DTOs;
namespace AuthService.Application.Interface.IRepositories;
/// <summary>Query-only read access; implementations must not mutate business state.</summary>
public interface IUserReadRepository
{
    Task<UserResponse?> FindByIdAsync(Guid userId, CancellationToken ct = default);
}
