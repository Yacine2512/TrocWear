import { AbstractControl, ValidationErrors } from "@angular/forms";
import { isGuidOrEmpty } from "../models/metier.model";

/** Champ vide accepté (Guid? = null côté API), sinon format GUID exigé. */
export const guidValidator = (c: AbstractControl): ValidationErrors | null =>
  isGuidOrEmpty(c.value) ? null : { guid: true };
