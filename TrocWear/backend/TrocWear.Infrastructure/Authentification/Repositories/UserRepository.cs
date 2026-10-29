using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using AuthService.Domain.Models;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AuthService.Infrastructure.Repositories;
public sealed class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct) =>
        db.Users.AnyAsync(x => x.Email == normalizedEmail, ct);
    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, ct);
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
    public void Add(User user) => db.Users.Add(user);
}
