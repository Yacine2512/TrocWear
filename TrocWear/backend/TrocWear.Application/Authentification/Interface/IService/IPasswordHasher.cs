namespace AuthService.Application.Interface.IService;
public interface IPasswordHasher
{
    string Hash(string plaintext);
    bool Verify(string plaintext, string storedHash);
}
