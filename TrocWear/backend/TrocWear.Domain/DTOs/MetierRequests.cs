namespace TrocWear.Domain.DTOs;
// Description et champs de liaison optionnels : avec [ApiController] + Nullable, un "string" non nullable
// est implicitement [Required] et une description vide renvoyait 400.
public sealed record MetierRequest(string Titre, string? Description, long PrixCentimes, Guid? AnnonceId, Guid? AcheteurId, Guid? VendeurId, string? Adresse);
public sealed record StatutRequest(string Statut);
