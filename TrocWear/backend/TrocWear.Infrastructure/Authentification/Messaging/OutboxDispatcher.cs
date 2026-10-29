using System.Text.Json;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrocWear.BuildingBlocks.EventContracts;
using TrocWear.BuildingBlocks.Messaging;
namespace AuthService.Infrastructure.Messaging;
public sealed class OutboxDispatcher(IServiceScopeFactory scopes, ILogger<OutboxDispatcher> log) : BackgroundService
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DispatchBatch(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogError(ex, "Échec du dispatcher d'outbox"); }
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
    private async Task DispatchBatch(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Un seul dispatcher à la fois, même lorsque plusieurs instances d'AuthService tournent.
        var locked = await db.Database.SqlQueryRaw<bool>(
            "SELECT pg_try_advisory_xact_lock(718204) AS \"Value\"").SingleAsync(ct);
        if (!locked) { await tx.RollbackAsync(ct); return; }
        var now = DateTimeOffset.UtcNow;
        var events = await db.OutboxMessages
            .Where(x => x.PublishedAt == null && x.NextAttemptAt <= now)
            .OrderBy(x => x.CreatedAt).Take(30).ToListAsync(ct);
        foreach (var row in events)
        {
            try
            {
                var message = JsonSerializer.Deserialize<EventEnvelope>(row.Payload, Options)
                    ?? throw new InvalidOperationException("Invalid outbox payload");
                await publisher.PublishAsync(message, ct);
                row.PublishedAt = now;
                row.LastError = null;
            }
            catch (Exception)
            {
                row.Attempts++;
                row.LastError = "Échec de publication (voir journaux techniques)";
                row.NextAttemptAt = now.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(row.Attempts, 8))));
                log.LogWarning("Publication reportée pour l'événement {EventId} (tentative {Attempt})", row.Id, row.Attempts);
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
