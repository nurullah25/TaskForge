import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { RealtimeService } from './realtime.service';

// Tells the API which live connection is making the change, so the server can send the
// resulting event to everyone except this browser, which already updated itself.
export const connectionIdInterceptor: HttpInterceptorFn = (request, next) => {
  const connectionId = inject(RealtimeService).currentConnectionId();

  if (!connectionId || !request.url.startsWith('/api/')) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { 'X-Connection-Id': connectionId } }));
};
