import { Component, inject, signal } from "@angular/core";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { MetierService } from "../../../core/api/metier.service";
import { apiErrorMessage } from "../../../core/api/api-error";
import { eurosToCentimes } from "../../../shared/models/metier.model";

/** POST /api/annonces — { titre, description, prixCentimes }. */
@Component({
  standalone: true,
  selector: "app-nouvelle-annonce",
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: "./nouvelle-annonce.component.html",
  styleUrl: "./nouvelle-annonce.component.scss",
})
export class NouvelleAnnonceComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MetierService);
  private readonly router = inject(Router);
  readonly loading = signal(false);
  readonly error = signal("");
  readonly form = this.fb.nonNullable.group({
    titre: ["", [Validators.required, Validators.maxLength(200)]],
    description: ["", Validators.maxLength(2000)],
    prix: [
      "",
      [Validators.required, Validators.pattern(/^\d+([.,]\d{1,2})?$/)],
    ],
  });

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading()) return;
    const { titre, description, prix } = this.form.getRawValue();
    const prixCentimes = eurosToCentimes(prix);
    if (!Number.isFinite(prixCentimes) || prixCentimes < 0) {
      this.error.set("Prix invalide.");
      return;
    }
    this.loading.set(true);
    this.error.set("");
    this.api
      .create("annonces", {
        titre: titre.trim(),
        description: description.trim() || null,
        prixCentimes,
        annonceId: null,
        acheteurId: null,
        vendeurId: null,
        adresse: null,
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          void this.router.navigateByUrl("/annonces");
        },
        error: (e) => {
          this.loading.set(false);
          this.error.set(apiErrorMessage(e, "Publication impossible."));
        },
      });
  }
}
