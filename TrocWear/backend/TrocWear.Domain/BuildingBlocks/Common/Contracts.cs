namespace TrocWear.BuildingBlocks.Common;
public sealed record AggregateIdentity(string Type, string Id, long Version);
public sealed record CommandEnvelope(Guid CommandeId, string Type, string Service, string? CibleId, long? VersionAttendue, System.Text.Json.JsonElement Donnees);
public sealed record DefaultAddress(string Ligne1,string CodePostal,string Ville,string Pays);
