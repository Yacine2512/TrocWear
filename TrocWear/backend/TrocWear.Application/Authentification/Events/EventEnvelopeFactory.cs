using System.Text.Json;
using TrocWear.BuildingBlocks.Common;
using TrocWear.BuildingBlocks.EventContracts;
using AuthService.Domain.Models;
namespace AuthService.Application.Events;
public static class EventEnvelopeFactory
{
    public static EventEnvelope UserCreated(User user, DateTimeOffset now) => Build(
        EventTypes.UtilisateurCree, user, now, ["utilisateurId", "pseudonyme"]);
    public static EventEnvelope ProfileModified(User user, DateTimeOffset now) => Build(
        EventTypes.ProfilPublicModifie, user, now, ["pseudonyme"]);
    private static EventEnvelope Build(string type, User user, DateTimeOffset now, string[] modified) => new(
        Guid.NewGuid(), type, 1, now, "authentification", ["annonces", "negociations"],
        new AggregateIdentity("Utilisateur", user.Id.ToString(), user.Version), modified,
        JsonSerializer.SerializeToElement(new { utilisateurId = user.Id, pseudonyme = user.Pseudonyme }));
}
