import { Component, inject, signal, viewChild } from "@angular/core";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { MetierService } from "../../../core/api/metier.service";
import { apiErrorMessage } from "../../../core/api/api-error";
import { MetierListComponent } from "../../../shared/components/metier-list/metier-list.component";
import { guidValidator } from "../../../shared/validators/guid.validator";
import {
  eurosToCentimes,
  optionalGuid,
} from "../../../shared/models/metier.model";

/** POST /api/negociations — { titre, description, prixCentimes, annonceId, vendeurId }. */
@Component({
  standalone: true,
  selector: "app-negociations",
  imports: [ReactiveFormsModule, MetierListComponent],
  templateUrl: "./negociations.component.html",
  styleUrl: "./negociations.component.scss",
})
export class NegociationsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MetierService);
  private readonly list = viewChild.required(MetierListComponent);
  readonly loading = signal(false);
  readonly error = signal("");
  readonly success = signal("");
  readonly form = this.fb.nonNullable.group({
    titre: ["", [Validators.required, Validators.maxLength(200)]],
    description: ["", Validators.maxLength(2000)],
    montant: [
      "",
      [Validators.required, Validators.pattern(/^\d+([.,]\d{1,2})?$/)],
    ],
    annonceId: ["", guidValidator],
    vendeurId: ["", guidValidator],
  });

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading()) return;
    const v = this.form.getRawValue();
    const prixCentimes = eurosToCentimes(v.montant);
    if (!Number.isFinite(prixCentimes) || prixCentimes < 0) {
      this.error.set("Montant invalide.");
      return;
    }
    this.loading.set(true);
    this.error.set("");
    this.success.set("");
    this.api
      .create("negociations", {
        titre: v.titre.trim(),
        description: v.description.trim() || null,
        prixCentimes,
        annonceId: optionalGuid(v.annonceId),
        acheteurId: null,
        vendeurId: optionalGuid(v.vendeurId),
        adresse: null,
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.success.set("Proposition enregistrée.");
          this.form.reset();
          this.list().reload();
        },
        error: (e) => {
          this.loading.set(false);
          this.error.set(apiErrorMessage(e, "Enregistrement impossible."));
        },
      });
  }
}
