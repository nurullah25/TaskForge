import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthResponse } from './auth.models';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => httpMock.verify());

  function signIn(token: string): void {
    auth.login({ email: 'sarah@example.com', password: 'secret' }).subscribe();
    httpMock.expectOne('/api/auth/login').flush(authResponse(token));
  }

  it('adds the access token to API requests', () => {
    signIn('token-1');

    http.get('/api/projects').subscribe();

    const request = httpMock.expectOne('/api/projects');
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-1');
    request.flush([]);
  });

  it('refreshes once when several requests fail with 401, then retries them', () => {
    signIn('expired-token');
    const results: string[] = [];

    http.get<string>('/api/first').subscribe((value) => results.push(value));
    http.get<string>('/api/second').subscribe((value) => results.push(value));
    httpMock.expectOne('/api/first').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/second').flush(null, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectOne('/api/auth/refresh').flush(authResponse('fresh-token'));

    const retriedFirst = httpMock.expectOne('/api/first');
    expect(retriedFirst.request.headers.get('Authorization')).toBe('Bearer fresh-token');
    retriedFirst.flush('first');
    httpMock.expectOne('/api/second').flush('second');

    expect(results).toEqual(['first', 'second']);
  });

  it('sends the user to the login page when the refresh fails', () => {
    signIn('expired-token');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    http.get('/api/projects').subscribe({ error: () => {} });
    httpMock.expectOne('/api/projects').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isSignedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.objectContaining({ queryParams: expect.anything() }));
  });
});

function authResponse(token: string): AuthResponse {
  return {
    accessToken: token,
    expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    user: { id: 1, email: 'sarah@example.com', fullName: 'Sarah Khan' },
  };
}
