import { Component, OnInit, inject, signal, viewChild } from "@angular/core";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { AuthService } from "../../../core/auth/auth.service";
import { MetierService } from "../../../core/api/metier.service";
import { apiErrorMessage } from "../../../core/api/api-error";
import { MetierListComponent } from "../../../shared/components/metier-list/metier-list.component";
import { guidValidator } from "../../../shared/validators/guid.validator";
import {
  eurosToCentimes,
  optionalGuid,
} from "../../../shared/models/metier.model";

/** POST /api/livraisons — { titre, description, prixCentimes (frais), adresse, annonceId, acheteurId, vendeurId }. */
@Component({
  standalone: true,
  selector: "app-livraisons",
  imports: [ReactiveFormsModule, MetierListComponent],
  templateUrl: "./livraisons.component.html",
  styleUrl: "./livraisons.component.scss",
})
export class LivraisonsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MetierService);
  private readonly auth = inject(AuthService);
  private readonly list = viewChild.required(MetierListComponent);
  readonly loading = signal(false);
  readonly error = signal("");
  readonly success = signal("");
  readonly form = this.fb.nonNullable.group({
    titre: ["", [Validators.required, Validators.maxLength(200)]],
    adresse: ["", [Validators.required, Validators.maxLength(1000)]],
    frais: [
      "0",
      [Validators.required, Validators.pattern(/^\d+([.,]\d{1,2})?$/)],
    ],
    annonceId: ["", guidValidator],
    acheteurId: ["", guidValidator],
    vendeurId: ["", guidValidator],
    description: ["", Validators.maxLength(2000)],
  });

  ngOnInit(): void {
    // Pré-remplit avec l'adresse par défaut du profil (UserResponse.adresseParDefaut).
    const adresse = this.auth.user()?.adresseParDefaut;
    if (adresse) this.form.controls.adresse.setValue(adresse);
  }

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading()) return;
    const v = this.form.getRawValue();
    const prixCentimes = eurosToCentimes(v.frais);
    if (!Number.isFinite(prixCentimes) || prixCentimes < 0) {
      this.error.set("Frais invalides.");
      return;
    }
    this.loading.set(true);
    this.error.set("");
    this.success.set("");
    this.api
      .create("livraisons", {
        titre: v.titre.trim(),
        description: v.description.trim() || null,
        prixCentimes,
        annonceId: optionalGuid(v.annonceId),
        acheteurId: optionalGuid(v.acheteurId),
        vendeurId: optionalGuid(v.vendeurId),
        adresse: v.adresse.trim(),
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.success.set("Livraison créée.");
          this.form.reset();
          this.ngOnInit();
          this.list().reload();
        },
        error: (e) => {
          this.loading.set(false);
          this.error.set(
            apiErrorMessage(e, "Création de la livraison impossible."),
          );
        },
      });
  }
}
