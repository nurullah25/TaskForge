import { HttpErrorResponse } from '@angular/common/http';
import { getErrorMessage } from './api-error';

describe('getErrorMessage', () => {
  function error(status: number, body: unknown): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: body });
  }

  it('explains a failed connection in plain words', () => {
    expect(getErrorMessage(error(0, null))).toContain("Can't reach the server");
  });

  it('uses the detail from the API', () => {
    const message = getErrorMessage(
      error(409, { title: 'Conflict', detail: 'That key is taken.' }),
    );

    expect(message).toBe('That key is taken.');
  });

  it('prefers the first validation message when there is one', () => {
    const message = getErrorMessage(
      error(400, { title: 'Bad request', errors: { Title: ['Title is required'] } }),
    );

    expect(message).toBe('Title is required');
  });

  it('falls back when the response says nothing useful', () => {
    expect(getErrorMessage(error(500, null))).toContain('Something went wrong');
    expect(getErrorMessage('not an http error')).toContain('Something went wrong');
  });
});
