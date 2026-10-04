import { EMPTY, expand, Observable, switchMap, timer } from 'rxjs';

export const POLL_INTERVAL_MS = 3000;

/**
 * Requests once, then again after each interval for as long as the latest response says
 * there is more to wait for. Polling stops by itself once processing settles.
 */
export function pollWhile<T>(
  request: () => Observable<T>,
  shouldContinue: (value: T) => boolean,
  intervalMs = POLL_INTERVAL_MS,
): Observable<T> {
  return request().pipe(
    expand((value) => (shouldContinue(value) ? timer(intervalMs).pipe(switchMap(request)) : EMPTY)),
  );
}
