namespace AuthService.Domain.Events;
public sealed record UserCreated(Guid UserId, long Version);
public sealed record UserPublicProfileChanged(Guid UserId, long Version);
