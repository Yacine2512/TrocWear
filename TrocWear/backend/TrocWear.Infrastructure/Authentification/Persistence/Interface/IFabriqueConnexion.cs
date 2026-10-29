using System.Data.Common;
namespace AuthService.Infrastructure.Persistence.Interface;
public interface IFabriqueConnexion
{
    DbConnection Create();
}
