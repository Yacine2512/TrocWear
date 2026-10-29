using AuthService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace AuthService.Infrastructure.Persistence.Migrations;
[DbContext(typeof(AuthDbContext))]
public sealed class AuthDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.3");
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(x=>x.Id);
            b.Property(x=>x.Pseudonyme).HasMaxLength(60).IsRequired();
            b.Property(x=>x.Email).HasMaxLength(254).IsRequired();
            b.HasIndex(x=>x.Email).IsUnique();
            b.Property(x=>x.PasswordHash).IsRequired();
            b.Property(x=>x.AdresseParDefaut).HasMaxLength(1000);
            b.Property(x=>x.Status).HasMaxLength(32).IsRequired();
            b.Property(x=>x.Version).IsConcurrencyToken();
            b.Property(x=>x.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(x=>x.UpdatedAt).HasColumnType("timestamp with time zone");
        });
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");b.HasKey(x=>x.Id);
            b.Property(x=>x.EventType).HasMaxLength(100).IsRequired();
            b.Property(x=>x.Payload).HasColumnType("jsonb").IsRequired();
            b.Property(x=>x.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(x=>x.PublishedAt).HasColumnType("timestamp with time zone");
            b.Property(x=>x.NextAttemptAt).HasColumnType("timestamp with time zone");
            b.Property(x=>x.LastError).HasMaxLength(1000);
            b.HasIndex(x=>new {x.PublishedAt,x.NextAttemptAt});
        });
    }
}
