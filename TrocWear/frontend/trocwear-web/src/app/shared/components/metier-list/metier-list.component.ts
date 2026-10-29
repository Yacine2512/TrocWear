import { Component, OnInit, inject, input, signal } from "@angular/core";
import { CurrencyPipe, DatePipe } from "@angular/common";
import { MetierService } from "../../../core/api/metier.service";
import { apiErrorMessage } from "../../../core/api/api-error";
import { MetierDocument, MetierType, STATUTS } from "../../models/metier.model";

/** Liste des éléments de l'utilisateur connecté (GET /api/{type}), avec PATCH statut et DELETE. */
@Component({
  standalone: true,
  selector: "app-metier-list",
  imports: [CurrencyPipe, DatePipe],
  templateUrl: "./metier-list.component.html",
  styleUrl: "./metier-list.component.scss",
})
export class MetierListComponent implements OnInit {
  private readonly api = inject(MetierService);
  readonly type = input.required<MetierType>();
  readonly montantLabel = input("Prix");
  readonly emptyText = input("Aucun élément pour le moment.");
  readonly items = signal<MetierDocument[]>([]);
  readonly loading = signal(false);
  readonly busyId = signal<string | null>(null);
  readonly error = signal("");

  statuts(): string[] {
    return STATUTS[this.type()];
  }

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set("");
    this.api.list(this.type()).subscribe({
      next: (items) => {
        this.items.set(
          [...items].sort((a, b) => b.createdAt.localeCompare(a.createdAt)),
        );
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(e, "Chargement impossible."));
      },
    });
  }

  changeStatus(item: MetierDocument, statut: string): void {
    if (!statut || statut === item.statut) return;
    this.busyId.set(item.id);
    this.error.set("");
    this.api.changeStatus(this.type(), item.id, statut).subscribe({
      next: (updated) => {
        this.items.update((list) =>
          list.map((x) => (x.id === updated.id ? updated : x)),
        );
        this.busyId.set(null);
      },
      error: (e) => {
        this.busyId.set(null);
        this.error.set(
          apiErrorMessage(e, "Modification du statut impossible."),
        );
      },
    });
  }

  remove(item: MetierDocument): void {
    if (!confirm(`Supprimer « ${item.titre} » ?`)) return;
    this.busyId.set(item.id);
    this.error.set("");
    this.api.delete(this.type(), item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((x) => x.id !== item.id));
        this.busyId.set(null);
      },
      error: (e) => {
        this.busyId.set(null);
        this.error.set(apiErrorMessage(e, "Suppression impossible."));
      },
    });
  }
}
