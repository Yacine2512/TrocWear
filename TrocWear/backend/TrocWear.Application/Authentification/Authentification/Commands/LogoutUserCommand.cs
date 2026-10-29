using MediatR;
using TrocWear.BuildingBlocks.CQRS;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;

namespace AuthService.Application.Authentification;

public sealed record LogoutUserCommand(string Token) : ICommand<bool>, IRequest<bool>;

public sealed class LogoutUserCommandHandler(ISessionService sessions)
    : ICommandHandler<LogoutUserCommand, bool>, IRequestHandler<LogoutUserCommand, bool>
{
    public async Task<bool> Handle(LogoutUserCommand command, CancellationToken ct = default)
    { await sessions.RevokeAsync(command.Token, ct); return true; }
}
