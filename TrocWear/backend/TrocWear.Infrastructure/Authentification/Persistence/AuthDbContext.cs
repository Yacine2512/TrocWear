using AuthService.Domain.Models;
using Microsoft.EntityFrameworkCore;
namespace AuthService.Infrastructure.Persistence;
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        var u = model.Entity<User>();
        u.ToTable("users"); u.HasKey(x => x.Id);
        u.Property(x => x.Pseudonyme).HasMaxLength(60).IsRequired();
        u.Property(x => x.Email).HasMaxLength(254).IsRequired();
        u.HasIndex(x => x.Email).IsUnique();
        u.Property(x => x.PasswordHash).IsRequired();
        u.Property(x => x.AdresseParDefaut).HasMaxLength(1000);
        u.Property(x => x.Status).HasMaxLength(32).IsRequired();
        u.Property(x => x.Version).IsConcurrencyToken();
        u.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        u.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        var o = model.Entity<OutboxMessage>();
        o.ToTable("outbox_messages"); o.HasKey(x => x.Id);
        o.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        o.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        o.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        o.Property(x => x.PublishedAt).HasColumnType("timestamp with time zone");
        o.Property(x => x.NextAttemptAt).HasColumnType("timestamp with time zone");
        o.Property(x => x.LastError).HasMaxLength(1000);
        o.HasIndex(x => new { x.PublishedAt, x.NextAttemptAt });
    }
}
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
