using System.Security.Cryptography;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
namespace AuthService.Infrastructure.Service;
// PBKDF2-HMAC-SHA256, 600 000 itérations, sel 128 bits : solution éprouvée sans dépendance tierce.
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 600_000;
    public string Hash(string plaintext)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(plaintext, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public bool Verify(string plaintext, string storedHash)
    {
        try
        {
            var fields = storedHash.Split('$');
            if (fields.Length != 4 || fields[0] != "pbkdf2-sha256" ||
                !int.TryParse(fields[1], out int n) || n is < 100_000 or > 2_000_000) return false;
            var salt = Convert.FromBase64String(fields[2]);
            var expected = Convert.FromBase64String(fields[3]);
            if (salt.Length != 16 || expected.Length != 32) return false;
            var actual = Rfc2898DeriveBytes.Pbkdf2(plaintext, salt, n, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}
