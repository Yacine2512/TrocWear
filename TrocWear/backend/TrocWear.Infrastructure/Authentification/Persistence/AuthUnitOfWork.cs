using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using Microsoft.EntityFrameworkCore;
using TrocWear.BuildingBlocks.EventContracts;
using System.Text.Json;
namespace AuthService.Infrastructure.Persistence;
public sealed class AuthUnitOfWork(AuthDbContext db) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken ct)
    {
        // Utilisateur et outbox validés atomiquement dans une même transaction PostgreSQL.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
public sealed class PostgresOutboxWriter(AuthDbContext db, TimeProvider clock) : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public void Add(EventEnvelope evt)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = evt.EvenementId, EventType = evt.Type, Payload = JsonSerializer.Serialize(evt, JsonOptions),
            CreatedAt = clock.GetUtcNow(), NextAttemptAt = clock.GetUtcNow()
        });
    }
}
