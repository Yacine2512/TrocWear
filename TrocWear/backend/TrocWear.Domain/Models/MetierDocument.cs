namespace TrocWear.Domain.Models;
public sealed class MetierDocument {
 public Guid Id {get;set;}=Guid.NewGuid();
 public Guid OwnerId {get;set;}
 public string Titre {get;set;}="";
 public string Description {get;set;}="";
 public long PrixCentimes {get;set;}
 public string Statut {get;set;}="nouveau";
 public Guid? AnnonceId {get;set;}
 public Guid? AcheteurId {get;set;}
 public Guid? VendeurId {get;set;}
 public string? Adresse {get;set;}
 public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
 public DateTime UpdatedAt {get;set;}=DateTime.UtcNow;
}
