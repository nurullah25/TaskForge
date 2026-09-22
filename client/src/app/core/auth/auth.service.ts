import { HttpClient, HttpContext } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../http/api-error';
import { AuthResponse, LoginRequest, RegisterRequest, User } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // The access token lives only in memory. After a page reload the session is restored
  // from the HttpOnly refresh cookie, which JavaScript can't read.
  private accessToken: string | null = null;
  private refreshRequest$: Observable<AuthResponse> | null = null;

  private readonly user = signal<User | null>(null);
  readonly currentUser = this.user.asReadonly();
  readonly isSignedIn = computed(() => this.user() !== null);

  get token(): string | null {
    return this.accessToken;
  }

  login(request: LoginRequest): Observable<User> {
    return this.http
      .post<AuthResponse>('/api/auth/login', request, { context: silent() })
      .pipe(map((response) => this.startSession(response)));
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http
      .post<AuthResponse>('/api/auth/register', request, { context: silent() })
      .pipe(map((response) => this.startSession(response)));
  }

  // Several requests can fail with 401 at the same time when the token expires.
  // They all share this one refresh call instead of each starting their own.
  refresh(): Observable<AuthResponse> {
    this.refreshRequest$ ??= this.http
      .post<AuthResponse>('/api/auth/refresh', null, { context: silent() })
      .pipe(
        tap((response) => this.startSession(response)),
        finalize(() => (this.refreshRequest$ = null)),
        shareReplay(1),
      );

    return this.refreshRequest$;
  }

  // Runs once at startup. A failed refresh just means the user isn't signed in.
  restoreSession(): Observable<unknown> {
    return this.refresh().pipe(catchError(() => of(null)));
  }

  logout(): void {
    this.endSession();
    this.router.navigateByUrl('/login');

    this.http
      .post('/api/auth/logout', null, { context: silent() })
      .pipe(catchError(() => of(null)))
      .subscribe();
  }

  // Called when a refresh fails mid-session, e.g. the refresh token expired.
  handleSessionExpired(): void {
    const returnUrl = this.router.url;
    this.endSession();
    this.router.navigate(['/login'], { queryParams: { returnUrl, expired: true } });
  }

  private startSession(response: AuthResponse): User {
    this.accessToken = response.accessToken;
    this.user.set(response.user);
    return response.user;
  }

  private endSession(): void {
    this.accessToken = null;
    this.user.set(null);
  }
}

function silent(): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, true);
}
