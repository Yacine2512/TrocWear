using MediatR; using TrocWear.Domain.Models;
namespace TrocWear.Application.Metiers;
public sealed record FindMetierQuery(string Type, Guid Id):IRequest<MetierDocument?>;
public sealed record ListMetierQuery(string Type,Guid OwnerId):IRequest<List<MetierDocument>>;
public sealed class FindMetierHandler(IMetierRepository repo):IRequestHandler<FindMetierQuery,MetierDocument?> {public Task<MetierDocument?> Handle(FindMetierQuery q,CancellationToken ct)=>repo.Find(q.Type,q.Id,ct);}
public sealed class ListMetierHandler(IMetierRepository repo):IRequestHandler<ListMetierQuery,List<MetierDocument>> {public Task<List<MetierDocument>> Handle(ListMetierQuery q,CancellationToken ct)=>repo.List(q.Type,q.OwnerId,ct);}

