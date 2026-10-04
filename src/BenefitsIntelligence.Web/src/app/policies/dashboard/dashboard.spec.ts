import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { policySummary } from '../testing/policy-data';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(policies = [policySummary()]) {
    const fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();
    http.expectOne('/api/policies').flush(policies);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('lists each policy with its extracted details', async () => {
    const page = await render();

    const cells = [...page.querySelectorAll('tbody tr:first-child > *')].map((cell) =>
      cell.textContent?.trim(),
    );
    expect(cells[0]).toContain('Proposed policy');
    expect(cells.slice(1, 4)).toEqual(['NorthStar Health', 'Essentials Select', '£150']);
  });

  it('flags completed policies that need review', async () => {
    const page = await render([policySummary({ needsReview: true })]);

    expect(page.querySelector('app-status-badge')?.textContent?.trim()).toBe('Needs review');
  });

  it('invites an upload when there are no policies', async () => {
    const page = await render([]);

    expect(page.textContent).toContain('No policies have been uploaded yet.');
  });

  it('reports when policies cannot be loaded', async () => {
    const fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();
    http.expectOne('/api/policies').flush(null, { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[role=alert]')?.textContent).toContain(
      'could not be loaded',
    );
  });
});
