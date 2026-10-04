import { defer, of } from 'rxjs';

import { pollWhile } from './polling';

describe('pollWhile', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('requests again after each interval until the response says to stop', () => {
    const responses = ['Queued', 'Queued', 'Completed'];
    let requests = 0;
    const received: string[] = [];

    pollWhile(
      () => defer(() => of(responses[requests++])),
      (status) => status !== 'Completed',
      1000,
    ).subscribe((status) => received.push(status));

    expect(received).toEqual(['Queued']);

    vi.advanceTimersByTime(1000);
    expect(received).toEqual(['Queued', 'Queued']);

    vi.advanceTimersByTime(1000);
    expect(received).toEqual(['Queued', 'Queued', 'Completed']);

    vi.advanceTimersByTime(5000);
    expect(requests).toBe(3);
  });

  it('does not poll when the first response is final', () => {
    let requests = 0;

    pollWhile(
      () => defer(() => of(++requests)),
      () => false,
      1000,
    ).subscribe();

    vi.advanceTimersByTime(5000);
    expect(requests).toBe(1);
  });
});
