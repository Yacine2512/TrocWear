import { Component, inject } from "@angular/core";
import { RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { AuthService } from "./core/auth/auth.service";
@Component({
  selector: "app-root",
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.scss",
})
export class AppComponent {
  readonly auth = inject(AuthService);
  constructor() {
    // Restaure la session (cookie HTTP-only) pour afficher la navigation dès le chargement.
    this.auth.loadCurrent().subscribe();
  }
}
