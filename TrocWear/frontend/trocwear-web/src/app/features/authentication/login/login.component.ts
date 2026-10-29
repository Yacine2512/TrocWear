import { Component, inject, signal } from "@angular/core";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
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
  templateUrl: "./login.component.html",
  styleUrl: "./login.component.scss",
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly loading = signal(false);
  readonly error = signal("");
  readonly registered = signal(
    inject(ActivatedRoute).snapshot.queryParamMap.get("inscription") === "ok",
  );
  readonly form = this.fb.nonNullable.group({
    email: ["", [Validators.required, Validators.email]],
    password: ["", Validators.required],
  });
  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.loading()) return;
    this.error.set("");
    this.registered.set(false);
    this.loading.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () =>
        this.auth.loadCurrent().subscribe((user) => {
          this.loading.set(false);
          if (user) void this.router.navigateByUrl("/mon-compte");
          else this.error.set("Impossible de vérifier la session.");
        }),
      error: (e) => {
        this.loading.set(false);
        this.error.set(
          e.status === 401
            ? "Email ou mot de passe incorrect."
            : apiErrorMessage(e, "Connexion indisponible. Réessaie."),
        );
      },
    });
  }
}
