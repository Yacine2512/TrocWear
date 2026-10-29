using MediatR;
using TrocWear.BuildingBlocks.CQRS;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using TrocWear.BuildingBlocks.SharedExceptions;

namespace AuthService.Application.Authentification;

public sealed record LoginUserCommand(string Email, string Password) : ICommand<string>, IRequest<string>;

public sealed class LoginUserCommandHandler(IUserRepository users, IPasswordHasher hasher, ISessionService sessions)
    : ICommandHandler<LoginUserCommand, string>, IRequestHandler<LoginUserCommand, string>
{
    public async Task<string> Handle(LoginUserCommand command, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !hasher.Verify(command.Password, user.PasswordHash) || user.Status != "actif")
            throw new DomainException("Identifiants incorrects.", 401);
        return await sessions.CreateAsync(user.Id, ct);
    }
}
