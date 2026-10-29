using MongoDB.Driver;
using TrocWear.Domain.Models;
using TrocWear.Application.Metiers;
namespace TrocWear.Infrastructure.Metiers;
public sealed class MongoMetierRepository(IMongoClient mongo):IMetierRepository {
 private IMongoCollection<MetierDocument> Col(string type) { var db=type switch {"annonces"=>"trocwear_annonces","negociations"=>"trocwear_negociations","livraisons"=>"trocwear_livraisons",_=>throw new ArgumentException("Service inconnu")};return mongo.GetDatabase(db).GetCollection<MetierDocument>(type);}
 public async Task<MetierDocument> Save(string type,MetierDocument item,CancellationToken ct){item.UpdatedAt=DateTime.UtcNow; await Col(type).ReplaceOneAsync(x=>x.Id==item.Id,item,new ReplaceOptions{IsUpsert=true},ct);return item;}
 public Task<MetierDocument?> Find(string type,Guid id,CancellationToken ct)=>Col(type).Find(x=>x.Id==id).FirstOrDefaultAsync(ct)!;
 public Task<List<MetierDocument>> List(string type,Guid owner,CancellationToken ct)=>Col(type).Find(x=>x.OwnerId==owner).ToListAsync(ct);
 public async Task<bool> Delete(string type,Guid id,Guid owner,CancellationToken ct)=>(await Col(type).DeleteOneAsync(x=>x.Id==id && x.OwnerId==owner,ct)).DeletedCount>0;
}
