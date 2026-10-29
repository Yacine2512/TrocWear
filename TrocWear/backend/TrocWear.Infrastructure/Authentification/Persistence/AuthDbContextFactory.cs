using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace AuthService.Infrastructure.Persistence;
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=trocwear_auth;Username=trocwear;Password=CHANGE_ME";
        return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(cs).Options);
    }
}
