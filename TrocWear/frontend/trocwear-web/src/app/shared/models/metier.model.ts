// Miroir de TrocWear.Domain.Models.MetierDocument et TrocWear.Domain.DTOs.MetierRequest
// (sérialisation JSON ASP.NET Core en camelCase).
export type MetierType = "annonces" | "negociations" | "livraisons";

export interface MetierDocument {
  id: string;
  ownerId: string;
  titre: string;
  description: string;
  prixCentimes: number;
  statut: string;
  annonceId: string | null;
  acheteurId: string | null;
  vendeurId: string | null;
  adresse: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface MetierRequest {
  titre: string;
  description: string | null;
  prixCentimes: number;
  annonceId: string | null;
  acheteurId: string | null;
  vendeurId: string | null;
  adresse: string | null;
}

export interface StatutRequest {
  statut: string;
}

/** Statuts proposés dans l'interface ; le backend accepte toute chaîne non vide ("nouveau" par défaut). */
export const STATUTS: Record<MetierType, string[]> = {
  annonces: ["nouveau", "publiee", "reservee", "vendue", "retiree"],
  negociations: ["nouveau", "en_cours", "acceptee", "refusee", "annulee"],
  livraisons: ["nouveau", "preparee", "expediee", "livree", "annulee"],
};

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Convertit une saisie optionnelle en Guid? côté API ("" → null, sinon valeur brute). */
export function optionalGuid(value: string | null | undefined): string | null {
  const v = (value ?? "").trim();
  return v ? v : null;
}

export function isGuidOrEmpty(value: string | null | undefined): boolean {
  const v = (value ?? "").trim();
  return v === "" || GUID.test(v);
}

/** Euros saisis (ex. "23,50") → centimes entiers attendus par PrixCentimes (long). */
export function eurosToCentimes(
  value: string | number | null | undefined,
): number {
  const n = Number(String(value ?? "0").replace(",", "."));
  return Number.isFinite(n) ? Math.round(n * 100) : NaN;
}
