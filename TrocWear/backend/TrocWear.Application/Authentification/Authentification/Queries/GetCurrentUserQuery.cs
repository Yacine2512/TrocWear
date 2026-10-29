using MediatR;
using AuthService.Domain.DTOs;
using TrocWear.BuildingBlocks.CQRS;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using TrocWear.BuildingBlocks.SharedExceptions;

namespace AuthService.Application.Authentification;

public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<UserResponse>, IRequest<UserResponse>;

public sealed class GetCurrentUserQueryHandler(IUserReadRepository users)
    : IQueryHandler<GetCurrentUserQuery, UserResponse>, IRequestHandler<GetCurrentUserQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetCurrentUserQuery query, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(query.UserId, ct)
            ?? throw new DomainException("Session invalide.",401);
        return user;
    }
}
