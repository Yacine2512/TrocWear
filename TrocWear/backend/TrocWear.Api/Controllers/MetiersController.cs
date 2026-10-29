using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrocWear.Application.Metiers;
using TrocWear.Domain.Models;
using TrocWear.Domain.DTOs;
namespace TrocWear.Api.Controllers;
[ApiController][Authorize][Route("api/[controller]")]
public abstract class MetierController(ISender sender):ControllerBase {
 protected abstract string Type {get;}
 private Guid Me()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpPost] public async Task<IActionResult> Create(MetierRequest r,CancellationToken ct){if(string.IsNullOrWhiteSpace(r.Titre)||r.Titre.Trim().Length>200||r.PrixCentimes<0)return BadRequest("Titre / montant invalide");var result=await sender.Send(new SaveMetierCommand(Type,new MetierDocument{OwnerId=Me(),Titre=r.Titre.Trim(),Description=r.Description?.Trim()??"",PrixCentimes=r.PrixCentimes,AnnonceId=r.AnnonceId,AcheteurId=r.AcheteurId,VendeurId=r.VendeurId,Adresse=string.IsNullOrWhiteSpace(r.Adresse)?null:r.Adresse.Trim()}),ct);return Created($"/api/{Type}/{result.Id}",result);}
 [HttpGet] public Task<List<MetierDocument>> List(CancellationToken ct)=>sender.Send(new ListMetierQuery(Type,Me()),ct);
 [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id,CancellationToken ct){var r=await sender.Send(new FindMetierQuery(Type,id),ct);return r is null?NotFound():r.OwnerId!=Me()?Forbid():Ok(r);}
 [HttpPatch("{id:guid}/statut")]public async Task<IActionResult> ChangeStatus(Guid id,StatutRequest request,CancellationToken ct){var r=await sender.Send(new FindMetierQuery(Type,id),ct);if(r is null)return NotFound();if(r.OwnerId!=Me())return Forbid();if(string.IsNullOrWhiteSpace(request.Statut))return BadRequest("Statut invalide");r.Statut=request.Statut.Trim();return Ok(await sender.Send(new SaveMetierCommand(Type,r),ct));}
 [HttpDelete("{id:guid}")]public async Task<IActionResult> Delete(Guid id,CancellationToken ct)=>await sender.Send(new DeleteMetierCommand(Type,id,Me()),ct)?NoContent():NotFound();
}
public sealed class AnnoncesController(ISender s):MetierController(s){protected override string Type=>"annonces";}
public sealed class NegociationsController(ISender s):MetierController(s){protected override string Type=>"negociations";}
public sealed class LivraisonsController(ISender s):MetierController(s){protected override string Type=>"livraisons";}
