import { Component, inject, signal } from "@angular/core";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { AuthService } from "../../../core/auth/auth.service";
import { apiErrorMessage } from "../../../core/api/api-error";
@Component({
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: "./register.component.html",
  styleUrl: "./register.component.scss",
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly loading = signal(false);
  readonly error = signal("");
  readonly form = this.fb.nonNullable.group({
    pseudonyme: [
      "",
      [Validators.required, Validators.minLength(2), Validators.maxLength(60)],
    ],
    email: ["", [Validators.required, Validators.email]],
    password: [
      "",
      [
        Validators.required,
        Validators.minLength(12),
        Validators.maxLength(128),
      ],
    ],
    confirmation: ["", Validators.required],
    adresseParDefaut: ["", Validators.maxLength(1000)],
  });
  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading()) return;
    const { pseudonyme, email, password, confirmation, adresseParDefaut } =
      this.form.getRawValue();
    if (password !== confirmation) {
      this.error.set("Les mots de passe ne correspondent pas.");
      return;
    }
    this.loading.set(true);
    this.error.set("");
    this.auth
      .register({
        pseudonyme: pseudonyme.trim(),
        email: email.trim(),
        password,
        adresseParDefaut: adresseParDefaut.trim() || null,
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          void this.router.navigate(["/connexion"], {
            queryParams: { inscription: "ok" },
          });
        },
        error: (e) => {
          this.loading.set(false);
          this.error.set(
            e.status === 409
              ? "Cette adresse email est déjà utilisée."
              : apiErrorMessage(
                  e,
                  "Inscription impossible. Vérifie les champs.",
                ),
          );
        },
      });
  }
}
