namespace AuthService.Application.Interface.IService;
public interface IUnitOfWork { Task CommitAsync(CancellationToken ct); }
