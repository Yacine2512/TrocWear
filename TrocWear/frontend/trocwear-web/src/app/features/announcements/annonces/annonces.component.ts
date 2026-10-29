import { Component } from "@angular/core";
import { RouterLink } from "@angular/router";
import { MetierListComponent } from "../../../shared/components/metier-list/metier-list.component";

@Component({
  standalone: true,
  selector: "app-annonces",
  imports: [RouterLink, MetierListComponent],
  templateUrl: "./annonces.component.html",
  styleUrl: "./annonces.component.scss",
})
export class AnnoncesComponent {}
