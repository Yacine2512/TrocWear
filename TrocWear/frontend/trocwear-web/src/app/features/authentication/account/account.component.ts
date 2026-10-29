import { Component, inject, signal } from "@angular/core";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { HttpErrorResponse } from "@angular/common/http";
import { AuthService } from "../../../core/auth/auth.service";
import { apiErrorMessage } from "../../../core/api/api-error";
@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: "./account.component.html",
  styleUrl: "./account.component.scss",
})
export class AccountComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(false);
  readonly error = signal("");
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly profileError = signal("");
  readonly profileForm = this.fb.nonNullable.group({
    pseudonyme: [
      "",
      [Validators.required, Validators.minLength(2), Validators.maxLength(60)],
    ],
  });

  startEdit() {
    this.profileForm.setValue({
      pseudonyme: this.auth.user()?.pseudonyme ?? "",
    });
    this.profileError.set("");
    this.editing.set(true);
  }

  saveProfile() {
    this.profileForm.markAllAsTouched();
    const user = this.auth.user();
    if (!user || this.profileForm.invalid || this.saving()) return;
    this.saving.set(true);
    this.profileError.set("");
    this.auth
      .updateProfile({
        pseudonyme: this.profileForm.getRawValue().pseudonyme.trim(),
        versionAttendue: user.version,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.editing.set(false);
        },
        error: (e) => {
          this.saving.set(false);
          if (e instanceof HttpErrorResponse && e.status === 409) {
            // Version obsolète : on recharge le profil pour obtenir la version courante.
            this.auth.loadCurrent().subscribe();
            this.profileError.set(
              "Ton profil a été modifié ailleurs : il a été rechargé, réessaie.",
            );
          } else {
            this.profileError.set(
              apiErrorMessage(e, "Modification impossible."),
            );
          }
        },
      });
  }

  logout() {
    this.loading.set(true);
    this.error.set("");
    this.auth.logout().subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl("/connexion");
      },
      error: () => {
        this.loading.set(false);
        this.error.set(
          "Déconnexion impossible : vérifie la connexion au serveur.",
        );
      },
    });
  }
}
