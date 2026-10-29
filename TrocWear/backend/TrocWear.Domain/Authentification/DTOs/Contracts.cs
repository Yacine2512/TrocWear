using System.ComponentModel.DataAnnotations;
using AuthService.Domain.Models;
namespace AuthService.Domain.DTOs;
// Records positionnels : ASP.NET Core exige que les attributs de validation soient posés sur les
// PARAMÈTRES du constructeur (sans cible "property:"), sinon InvalidOperationException à la liaison du modèle.
public sealed record RegisterRequest(
    [Required, MinLength(2), MaxLength(60)] string Pseudonyme,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(12), MaxLength(128)] string Password,
    [MaxLength(1000)] string? AdresseParDefaut = null);
public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
public sealed record UpdateProfileRequest(
    [Required, MinLength(2), MaxLength(60)] string Pseudonyme,
    [Range(1, long.MaxValue)] long VersionAttendue);
public sealed record UserResponse(Guid Id, string Pseudonyme, string Email, string? AdresseParDefaut,
    string Status, long Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static UserResponse From(User user) => new(user.Id, user.Pseudonyme, user.Email,
       user.AdresseParDefaut, user.Status, user.Version, user.CreatedAt, user.UpdatedAt);
}
