import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import {
  MetierDocument,
  MetierRequest,
  MetierType,
  StatutRequest,
} from "../../shared/models/metier.model";

/** Client des contrôleurs AnnoncesController / NegociationsController / LivraisonsController. */
@Injectable({ providedIn: "root" })
export class MetierService {
  private readonly http = inject(HttpClient);

  create(type: MetierType, payload: MetierRequest): Observable<MetierDocument> {
    return this.http.post<MetierDocument>(`/api/${type}`, payload);
  }
  list(type: MetierType): Observable<MetierDocument[]> {
    return this.http.get<MetierDocument[]>(`/api/${type}`);
  }
  get(type: MetierType, id: string): Observable<MetierDocument> {
    return this.http.get<MetierDocument>(`/api/${type}/${id}`);
  }
  changeStatus(
    type: MetierType,
    id: string,
    statut: string,
  ): Observable<MetierDocument> {
    const body: StatutRequest = { statut };
    return this.http.patch<MetierDocument>(`/api/${type}/${id}/statut`, body);
  }
  delete(type: MetierType, id: string): Observable<void> {
    return this.http.delete<void>(`/api/${type}/${id}`);
  }
}
