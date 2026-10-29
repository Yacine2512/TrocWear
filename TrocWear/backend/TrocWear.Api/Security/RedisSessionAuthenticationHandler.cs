using System.Security.Claims;
using System.Text.Encodings.Web;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
namespace AuthService.Api.Security;
public sealed class RedisSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    ISessionService sessions, IUserRepository users)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue("trocwear.sid", out var token) || string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();
        var userId = await sessions.ResolveAsync(token, Context.RequestAborted);
        if (!userId.HasValue) return AuthenticateResult.Fail("Session expirée ou invalide.");
        var user = await users.GetByIdAsync(userId.Value, Context.RequestAborted);
        if (user is null || user.Status != "actif") return AuthenticateResult.Fail("Utilisateur indisponible.");
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Pseudonyme)], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
