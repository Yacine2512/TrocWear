using MediatR;
using AuthService.Domain.DTOs;
using TrocWear.BuildingBlocks.CQRS;
using AuthService.Application.Events;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using AuthService.Application.Authentification;
using TrocWear.BuildingBlocks.SharedExceptions;
using FluentValidation;

namespace AuthService.Application.Utilisateurs;

public sealed record UpdateProfileCommand(Guid UserId, string Pseudonyme, long VersionAttendue) : ICommand<UserResponse>, IRequest<UserResponse>;

public sealed class UpdateProfileCommandHandler(IUserRepository users, IOutboxWriter outbox,
    IUnitOfWork unitOfWork, ProfileValidator validator, TimeProvider clock)
    : ICommandHandler<UpdateProfileCommand, UserResponse>, IRequestHandler<UpdateProfileCommand, UserResponse>
{
    public async Task<UserResponse> Handle(UpdateProfileCommand command, CancellationToken ct = default)
    {
        validator.Validate(command.Pseudonyme, command.VersionAttendue);
        var user = await users.GetByIdAsync(command.UserId, ct)
            ?? throw new DomainException("Utilisateur introuvable.", 404);
        if (user.Version != command.VersionAttendue) throw new DomainException("Version obsolète.", 409);
        var now = clock.GetUtcNow();
        user.ModifyPublicProfile(command.Pseudonyme, now);
        outbox.Add(EventEnvelopeFactory.ProfileModified(user, now));
        await unitOfWork.CommitAsync(ct);
        return UserResponse.From(user);
    }
}

public sealed class ProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public ProfileValidator()
    {
        RuleFor(x => x.Pseudonyme).NotEmpty().MinimumLength(2).MaximumLength(60);
        RuleFor(x => x.VersionAttendue).GreaterThan(0);
    }

    public void Validate(string pseudonyme, long versionAttendue)
    {
        if (string.IsNullOrWhiteSpace(pseudonyme) || pseudonyme.Trim().Length is < 2 or > 60)
            throw new DomainException("Pseudonyme invalide.");
        if (versionAttendue <= 0) throw new DomainException("Version attendue invalide.");
    }
}
