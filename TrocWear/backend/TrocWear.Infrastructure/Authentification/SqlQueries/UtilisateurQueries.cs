namespace AuthService.Infrastructure.SqlQueries;

public static class UtilisateurQueries
{
    public const string ParId = """
        SELECT "Id", "Pseudonyme", "Email", "AdresseParDefaut", "Status",
               "Version", "CreatedAt", "UpdatedAt"
        FROM users WHERE "Id" = @UserId
        """;
}
