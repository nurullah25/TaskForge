import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { getErrorMessage, SKIP_ERROR_TOAST } from './api-error';

// Shows a snackbar for errors that the calling code doesn't handle itself.
// 400 is left to forms (they show field errors) and 401 to the auth interceptor.
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const snackBar = inject(MatSnackBar);

  return next(request).pipe(
    catchError((error) => {
      const handledElsewhere =
        request.context.get(SKIP_ERROR_TOAST) ||
        (error instanceof HttpErrorResponse && [400, 401].includes(error.status));

      if (!handledElsewhere) {
        snackBar.open(getErrorMessage(error), 'Dismiss', { duration: 6000 });
      }

      return throwError(() => error);
    }),
  );
};
