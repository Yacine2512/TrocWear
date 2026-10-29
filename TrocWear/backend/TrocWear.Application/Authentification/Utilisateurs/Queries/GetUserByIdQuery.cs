using MediatR;
using AuthService.Domain.DTOs;
using TrocWear.BuildingBlocks.CQRS;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using TrocWear.BuildingBlocks.SharedExceptions;

namespace AuthService.Application.Utilisateurs;

public sealed record GetUserByIdQuery(Guid RequestedId, Guid ConnectedUserId) : IQuery<UserResponse>, IRequest<UserResponse>;

public sealed class GetUserByIdQueryHandler(IUserReadRepository users)
    : IQueryHandler<GetUserByIdQuery, UserResponse>, IRequestHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetUserByIdQuery query, CancellationToken ct = default)
    {
        if (query.RequestedId != query.ConnectedUserId)
            throw new DomainException("Accès interdit.", 403);
        var user = await users.FindByIdAsync(query.RequestedId, ct)
            ?? throw new DomainException("Utilisateur introuvable.",404);
        return user;
    }
}
