using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
namespace TrocWear.Infrastructure.Metiers;
/// <summary>
/// MongoDB.Driver 3.x refuse de sérialiser un Guid tant que la représentation n'est pas fixée
/// ("GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified").
/// À appeler une seule fois au démarrage, avant toute utilisation du client Mongo.
/// </summary>
public static class MongoConfiguration
{
    private static int _registered;
    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1) return;
        var guid = new GuidSerializer(GuidRepresentation.Standard);
        BsonSerializer.TryRegisterSerializer<Guid>(guid);
        BsonSerializer.TryRegisterSerializer<Guid?>(new NullableSerializer<Guid>(guid));
    }
}
