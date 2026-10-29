namespace AuthService.Domain.ValueObjects;
public readonly record struct NormalizedEmail(string Value)
{
    public static NormalizedEmail From(string email) => new(email.Trim().ToLowerInvariant());
    public override string ToString() => Value;
}
