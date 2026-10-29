using System.Text.Json;
using TrocWear.BuildingBlocks.Common;
namespace TrocWear.BuildingBlocks.EventContracts;
public sealed record EventEnvelope(
    Guid EvenementId, string Type, int SchemaVersion, DateTimeOffset Date,
    string ServiceEmetteur, string[] ServicesRecepteurs, AggregateIdentity AgregatSource,
    string[] DonneesModifiees, JsonElement NouvellesValeurs);
public static class EventTypes
{
    public const string UtilisateurCree = nameof(UtilisateurCree);
    public const string ProfilPublicModifie = nameof(ProfilPublicModifie);
    public const string AnnoncePubliee = nameof(AnnoncePubliee);
    public const string AnnonceModifiee = nameof(AnnonceModifiee);
    public const string AnnonceReservee = nameof(AnnonceReservee);
    public const string ReservationLiberee = nameof(ReservationLiberee);
    public const string PrixAccepte = nameof(PrixAccepte);
    public const string AccordConfirme = nameof(AccordConfirme);
    public const string LivraisonCreee = nameof(LivraisonCreee);
    public const string LivraisonStatutModifie = nameof(LivraisonStatutModifie);
    public const string PropositionSoumise = nameof(PropositionSoumise);
    public const string PropositionRefusee = nameof(PropositionRefusee);
    public const string DestinationLivraisonModifiee = nameof(DestinationLivraisonModifiee);
}
public sealed record UtilisateurCree(Guid UtilisateurId, string Pseudonyme);
public sealed record ProfilPublicModifie(Guid UtilisateurId, string Pseudonyme);
public sealed record AnnoncePubliee(Guid AnnonceId, Guid VendeurId, long PrixCentimes);
public sealed record AnnonceModifiee(Guid AnnonceId, long Version);
public sealed record AnnonceReservee(Guid AnnonceId, Guid ReservationId);
public sealed record ReservationLiberee(Guid AnnonceId, Guid ReservationId);
public sealed record PrixAccepte(Guid NegociationId, long MontantCentimes);
public sealed record AccordConfirme(Guid NegociationId, Guid AcheteurId, Guid VendeurId);
public sealed record LivraisonCreee(Guid LivraisonId, Guid NegociationId);
public sealed record LivraisonStatutModifie(Guid LivraisonId, string Statut);
public sealed record PropositionSoumise(Guid NegociationId, Guid PropositionId, long MontantCentimes);
public sealed record PropositionRefusee(Guid NegociationId, Guid PropositionId);
public sealed record DestinationLivraisonModifiee(Guid LivraisonId, string Destination);
