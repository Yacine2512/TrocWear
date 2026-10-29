using MediatR;
namespace TrocWear.Application.Metiers;
public sealed record DeleteMetierCommand(string Type,Guid Id,Guid OwnerId):IRequest<bool>;
public sealed class DeleteMetierHandler(IMetierRepository repo):IRequestHandler<DeleteMetierCommand,bool> { public Task<bool> Handle(DeleteMetierCommand q,CancellationToken ct)=>repo.Delete(q.Type,q.Id,q.OwnerId,ct); }
