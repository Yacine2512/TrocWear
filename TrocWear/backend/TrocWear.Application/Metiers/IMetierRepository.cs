using MediatR;
using TrocWear.Domain.Models;
namespace TrocWear.Application.Metiers;
public interface IMetierRepository { Task<MetierDocument> Save(string type, MetierDocument item, CancellationToken ct); Task<MetierDocument?> Find(string type, Guid id, CancellationToken ct); Task<List<MetierDocument>> List(string type, Guid ownerId, CancellationToken ct); Task<bool> Delete(string type, Guid id, Guid ownerId, CancellationToken ct); }
