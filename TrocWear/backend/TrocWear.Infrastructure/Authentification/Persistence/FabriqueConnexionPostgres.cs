using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Npgsql;
using AuthService.Infrastructure.Persistence.Interface;
namespace AuthService.Infrastructure.Persistence;
public sealed class FabriqueConnexionPostgres(IConfiguration configuration) : IFabriqueConnexion
{
    public DbConnection Create() => new NpgsqlConnection(
        configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connexion PostgreSQL manquante."));
}
