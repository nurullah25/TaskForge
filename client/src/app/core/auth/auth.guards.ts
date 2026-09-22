import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

// Guards only improve navigation. The API enforces access on every request anyway.

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isSignedIn() || router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

// Keeps signed-in users away from the login and register pages.
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return !auth.isSignedIn() || router.createUrlTree(['/dashboard']);
};
