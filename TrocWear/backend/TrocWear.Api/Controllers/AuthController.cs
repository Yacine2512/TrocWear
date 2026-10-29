using MediatR;
using System.Security.Claims;
using AuthService.Application.Authentification;
using AuthService.Application.Utilisateurs;
using AuthService.Domain.DTOs;
using AuthService.Infrastructure.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrocWear.BuildingBlocks.CQRS;
namespace AuthService.Api.Controllers;
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender mediator, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request,CancellationToken ct)
    {
        var user = await mediator.Send(new RegisterUserCommand(request.Pseudonyme,request.Email,request.Password,request.AdresseParDefaut),ct);
        return Created($"/api/auth/users/{user.Id}", user);
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request,CancellationToken ct)
    {
        var token = await mediator.Send(new LoginUserCommand(request.Email, request.Password),ct);
        Response.Cookies.Append("trocwear.sid", token, CookieSettings());
        Response.Headers.CacheControl = "no-store";
        return Ok(new { message = "Connexion réussie" });
    }
    // Pas de [Authorize] : une session expirée doit pouvoir se déconnecter (le cookie est toujours supprimé).
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue("trocwear.sid", out var token) && !string.IsNullOrEmpty(token))
            await mediator.Send(new LogoutUserCommand(token),ct);
        Response.Cookies.Delete("trocwear.sid", CookieSettings());
        return NoContent();
    }
    [Authorize]
    [HttpGet("me")]
    public Task<UserResponse> Me(CancellationToken ct) => mediator.Send(new GetCurrentUserQuery(MyId()),ct);
    [Authorize]
    [HttpGet("users/{id:guid}")]
    public Task<UserResponse> GetById(Guid id,CancellationToken ct) => mediator.Send(new GetUserByIdQuery(id,MyId()),ct);
    [Authorize]
    [HttpPatch("profile")]
    public Task<UserResponse> UpdateProfile([FromBody] UpdateProfileRequest request,CancellationToken ct) =>
        mediator.Send(new UpdateProfileCommand(MyId(),request.Pseudonyme,request.VersionAttendue),ct);
    private Guid MyId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private CookieOptions CookieSettings() => new()
    {
        HttpOnly = true, Secure = !env.IsDevelopment(), SameSite = SameSiteMode.Lax,
        Path = "/", IsEssential = true, MaxAge = SessionService.Lifetime
    };
}
