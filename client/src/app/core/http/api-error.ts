import { HttpContextToken, HttpErrorResponse } from '@angular/common/http';
import { catchError, EMPTY, Observable } from 'rxjs';

// Shape of the RFC 7807 error responses returned by the API.
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

// Set on requests whose errors are shown inline by the caller (e.g. login form),
// so the global error interceptor doesn't show a second message.
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

export function getErrorMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.',
): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return "Can't reach the server. Check your connection and try again.";
  }

  const problem = error.error as ProblemDetails | null;
  const firstValidationError = problem?.errors ? Object.values(problem.errors)[0]?.[0] : undefined;

  return firstValidationError ?? problem?.detail ?? problem?.title ?? fallback;
}

// The error interceptor already showed a message, so the stream just stops here.
// Without this the failure would also surface as an unhandled error in the console.
export function ignoreHandledError<T>() {
  return (source: Observable<T>) => source.pipe(catchError(() => EMPTY));
}
