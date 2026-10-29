using MediatR; using TrocWear.Domain.Models; using TrocWear.Application.Metiers;
namespace TrocWear.Application.Metiers;
public sealed record SaveMetierCommand(string Type, MetierDocument Document):IRequest<MetierDocument>;
public sealed class SaveMetierHandler(IMetierRepository repo):IRequestHandler<SaveMetierCommand,MetierDocument> {public Task<MetierDocument> Handle(SaveMetierCommand cmd,CancellationToken ct)=>repo.Save(cmd.Type,cmd.Document,ct);}
