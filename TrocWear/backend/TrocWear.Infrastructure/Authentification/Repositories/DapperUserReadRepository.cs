using AuthService.Domain.DTOs;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using Dapper;
using AuthService.Infrastructure.Persistence.Interface;
using AuthService.Infrastructure.SqlQueries;
using AuthService.Infrastructure.Models;

namespace AuthService.Infrastructure.Repositories;

/// <summary>Parameterized, read-only PostgreSQL queries (CQRS query side).</summary>
public sealed class DapperUserReadRepository(IFabriqueConnexion connections) : IUserReadRepository
{

    public async Task<UserResponse?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var connection = connections.Create();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(UtilisateurQueries.ParId, new { UserId = userId }, cancellationToken: ct));
        return row is null ? null : new UserResponse(row.Id, row.Pseudonyme, row.Email,
            row.AdresseParDefaut, row.Status, row.Version, new DateTimeOffset(row.CreatedAt), new DateTimeOffset(row.UpdatedAt));
    }

}
