namespace AuthService.Infrastructure.Models;

public sealed class UserRow
{
    public Guid Id { get; set; }
    public string Pseudonyme { get; set; } = "";
    public string Email { get; set; } = "";
    public string? AdresseParDefaut { get; set; }
    public string Status { get; set; } = "";
    public long Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
