namespace AuthService.Domain.Models;
public sealed class User
{
    private User() { } // EF Core
    public Guid Id { get; private set; }
    public string Pseudonyme { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public string? AdresseParDefaut { get; private set; }
    public string Status { get; private set; } = "actif";
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public static User Create(string pseudonyme, string email, string hash, DateTimeOffset now,
        string? adresseParDefaut = null) => new()
    {
        Id = Guid.NewGuid(), Pseudonyme = pseudonyme.Trim(), Email = email.Trim().ToLowerInvariant(),
        PasswordHash = hash, AdresseParDefaut = string.IsNullOrWhiteSpace(adresseParDefaut) ? null : adresseParDefaut.Trim(),
        Version = 1, CreatedAt = now, UpdatedAt = now
    };
    public void ModifyPublicProfile(string pseudonyme, DateTimeOffset now)
    {
        Pseudonyme = pseudonyme.Trim(); Version++; UpdatedAt = now;
    }
}
