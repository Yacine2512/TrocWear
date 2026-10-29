import { Routes } from "@angular/router";
import { authGuard } from "./core/guards/auth.guard";
import { LoginComponent } from "./features/authentication/login/login.component";
import { RegisterComponent } from "./features/authentication/register/register.component";
import { AccountComponent } from "./features/authentication/account/account.component";
import { AnnoncesComponent } from "./features/announcements/annonces/annonces.component";
import { NouvelleAnnonceComponent } from "./features/announcements/nouvelle-annonce/nouvelle-annonce.component";
import { NegociationsComponent } from "./features/negotiation/negociations/negociations.component";
import { LivraisonsComponent } from "./features/delivery/livraisons/livraisons.component";

export const routes: Routes = [
  { path: "", redirectTo: "mon-compte", pathMatch: "full" },
  { path: "connexion", component: LoginComponent },
  { path: "inscription", component: RegisterComponent },
  { path: "mon-compte", component: AccountComponent, canActivate: [authGuard] },
  { path: "annonces", component: AnnoncesComponent, canActivate: [authGuard] },
  {
    path: "annonces/nouvelle",
    component: NouvelleAnnonceComponent,
    canActivate: [authGuard],
  },
  {
    path: "negociations",
    component: NegociationsComponent,
    canActivate: [authGuard],
  },
  {
    path: "livraisons",
    component: LivraisonsComponent,
    canActivate: [authGuard],
  },
  { path: "**", redirectTo: "mon-compte" },
];
