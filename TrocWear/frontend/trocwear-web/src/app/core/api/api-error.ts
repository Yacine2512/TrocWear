import { HttpErrorResponse } from "@angular/common/http";

/**
 * Extrait un message lisible des réponses de l'API :
 * problem+json du gestionnaire d'exceptions ({ title, detail }), ValidationProblemDetails
 * de [ApiController] ({ errors }), ou BadRequest("texte") des contrôleurs métiers.
 */
export function apiErrorMessage(e: unknown, fallback: string): string {
  if (!(e instanceof HttpErrorResponse)) return fallback;
  if (e.status === 0)
    return "Serveur injoignable. Vérifie que l'API est démarrée.";
  if (e.status === 401) return "Session expirée : reconnecte-toi.";
  const body = e.error;
  if (typeof body === "string" && body.trim()) return body;
  if (body && typeof body === "object") {
    const errors = (body as { errors?: Record<string, string[]> }).errors;
    if (errors) {
      const first = Object.values(errors).flat()[0];
      if (first) return first;
    }
    const detail = (body as { detail?: string }).detail;
    if (detail) return detail;
  }
  return fallback;
}
