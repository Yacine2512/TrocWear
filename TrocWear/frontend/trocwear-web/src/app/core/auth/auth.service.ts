import { Injectable, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { catchError, finalize, Observable, of, tap } from "rxjs";
import {
  RegisterRequest,
  UpdateProfileRequest,
  User,
} from "../../shared/models/user.model";
@Injectable({ providedIn: "root" })
export class AuthService {
  readonly user = signal<User | null>(null);
  constructor(private readonly http: HttpClient) {}
  register(payload: RegisterRequest): Observable<User> {
    return this.http.post<User>("/api/auth/register", payload);
  }
  login(email: string, password: string): Observable<unknown> {
    return this.http.post("/api/auth/login", { email, password });
  }
  loadCurrent(): Observable<User | null> {
    return this.http.get<User>("/api/auth/me").pipe(
      tap((user) => this.user.set(user)),
      catchError(() => {
        this.user.set(null);
        return of(null);
      }),
    );
  }
  updateProfile(payload: UpdateProfileRequest): Observable<User> {
    return this.http
      .patch<User>("/api/auth/profile", payload)
      .pipe(tap((user) => this.user.set(user)));
  }
  logout(): Observable<unknown> {
    // L'utilisateur local est effacé même si l'appel échoue (session déjà expirée…).
    return this.http
      .post("/api/auth/logout", {})
      .pipe(finalize(() => this.user.set(null)));
  }
}
