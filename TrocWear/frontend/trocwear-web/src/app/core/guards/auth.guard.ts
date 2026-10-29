import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { map } from "rxjs";
import { AuthService } from "../auth/auth.service";
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth
    .loadCurrent()
    .pipe(map((user) => (user ? true : router.createUrlTree(["/connexion"]))));
};
