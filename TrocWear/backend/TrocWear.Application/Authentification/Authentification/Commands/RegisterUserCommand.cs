using MediatR;
using AuthService.Domain.DTOs;
using TrocWear.BuildingBlocks.CQRS;
using AuthService.Application.Events;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using AuthService.Application.Utilisateurs;
using AuthService.Domain.Models;
using TrocWear.BuildingBlocks.SharedExceptions;
using System.Net.Mail;
using FluentValidation;

namespace AuthService.Application.Authentification;

public sealed record RegisterUserCommand(string Pseudonyme, string Email, string Password, string? AdresseParDefaut = null) : ICommand<UserResponse>, IRequest<UserResponse>;

public sealed class RegisterUserCommandHandler(
    IUserRepository users, IPasswordHasher hasher, IOutboxWriter outbox,
    IUnitOfWork unitOfWork, RegisterUserValidator validator, TimeProvider clock)
    : ICommandHandler<RegisterUserCommand, UserResponse>, IRequestHandler<RegisterUserCommand, UserResponse>
{
    public async Task<UserResponse> Handle(RegisterUserCommand command, CancellationToken ct = default)
    {
        validator.Validate(command.Pseudonyme, command.Email, command.Password, command.AdresseParDefaut);
        var email = command.Email.Trim().ToLowerInvariant();
        if (await users.EmailExistsAsync(email, ct)) throw new DomainException("Email déjà utilisé.", 409);
        var now = clock.GetUtcNow();
        var user = User.Create(command.Pseudonyme, email, hasher.Hash(command.Password), now, command.AdresseParDefaut);
        users.Add(user);
        outbox.Add(EventEnvelopeFactory.UserCreated(user, now));
        await unitOfWork.CommitAsync(ct);
        return UserResponse.From(user);
    }
}

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Pseudonyme).NotEmpty().MinimumLength(2).MaximumLength(60);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        RuleFor(x => x.AdresseParDefaut).MaximumLength(1000);
    }

    public void Validate(string pseudonyme,string email,string password,string? adresseParDefaut = null)
    {
        if (adresseParDefaut is { Length: > 1000 })
            throw new DomainException("Adresse : 1000 caractères maximum.");
        if (string.IsNullOrWhiteSpace(pseudonyme) || pseudonyme.Trim().Length is < 2 or > 60)
            throw new DomainException("Pseudonyme : entre 2 et 60 caractères.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
            !MailAddress.TryCreate(email, out var addr) || addr.Address != email.Trim())
            throw new DomainException("Email invalide.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
            throw new DomainException("Le mot de passe doit avoir entre 12 et 128 caractères.");
    }
}
