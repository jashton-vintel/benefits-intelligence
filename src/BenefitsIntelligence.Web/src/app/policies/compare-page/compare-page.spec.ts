import { registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import localeEnGb from '@angular/common/locales/en-GB';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ComparisonReport } from '../policy.models';
import { policySummary } from '../testing/policy-data';
import { ComparePage } from './compare-page';

const REPORT: ComparisonReport = {
  current: {
    id: 'a1',
    name: 'Current policy',
    provider: 'Atlas Healthcare',
    schemeName: 'Corporate Plus',
  },
  proposed: {
    id: 'b2',
    name: 'Proposed policy',
    provider: 'NorthStar Health',
    schemeName: 'Essentials Select',
  },
  differences: [
    {
      field: 'annual_premium',
      kind: 'Money',
      current: '120000.00',
      proposed: '108000.00',
      delta: -12000,
      change: 'Decreased',
      needsReview: false,
    },
    {
      field: 'dependants_allowed',
      kind: 'Flag',
      current: 'true',
      proposed: 'true',
      delta: null,
      change: 'Unchanged',
      needsReview: false,
    },
    {
      field: 'dependants_included',
      kind: 'Flag',
      current: 'true',
      proposed: null,
      delta: null,
      change: 'Unknown',
      needsReview: true,
    },
  ],
  summary: null,
};

describe('ComparePage', () => {
  let http: HttpTestingController;

  beforeAll(() => registerLocaleData(localeEnGb));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'en-GB' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(current = 'a1', proposed = 'b2') {
    const fixture = TestBed.createComponent(ComparePage);
    fixture.componentRef.setInput('current', current);
    fixture.componentRef.setInput('proposed', proposed);
    await fixture.whenStable();

    http
      .expectOne('/api/policies')
      .flush([policySummary({ id: 'a1', name: 'Current policy' }), policySummary({ id: 'b2' })]);
    await fixture.whenStable();
    return fixture;
  }

  function comparisonRequests() {
    return http.match((request) => request.url === '/api/comparisons');
  }

  async function respond(fixture: ComponentFixture<ComparePage>, summary: string | null) {
    const [table, written] = comparisonRequests();
    expect(table.request.params.get('includeSummary')).toBe('false');
    expect(written.request.params.get('includeSummary')).toBe('true');

    table.flush(REPORT);
    await fixture.whenStable();
    return { written, finish: () => written.flush({ ...REPORT, summary }) };
  }

  it('shows the calculated differences before the summary has been written', async () => {
    const fixture = await render();
    await respond(fixture, null);
    const page = fixture.nativeElement as HTMLElement;

    const premium = [...page.querySelectorAll('tbody tr')].find((row) =>
      row.textContent?.includes('Annual premium'),
    )!;
    expect(premium.textContent).toContain('£120,000');
    expect(premium.textContent).toContain('£108,000');
    expect(premium.textContent).toContain('−£12,000');
    expect(page.querySelector('.writing')).not.toBeNull();
  });

  it('says when no summary could be written', async () => {
    const fixture = await render();
    const { finish } = await respond(fixture, null);

    finish();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.summary').textContent).toContain(
      'not available right now',
    );
  });

  it('renders a written summary', async () => {
    const fixture = await render();
    const [table, written] = comparisonRequests();
    table.flush(REPORT);
    written.flush({ ...REPORT, summary: 'The annual premium falls from £120,000 to £108,000.' });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.summary-text').textContent).toContain(
      'falls from £120,000 to £108,000',
    );
  });

  it('flags differences that rely on facts needing review', async () => {
    const fixture = await render();
    await respond(fixture, null);
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('.alert.warning')?.textContent).toContain('1 difference relies');
    expect(page.querySelector('tr.needs-review')?.textContent).toContain('Cannot compare');
  });

  it('can hide fields that have not changed', async () => {
    const fixture = await render();
    await respond(fixture, null);
    const page = fixture.nativeElement as HTMLElement;

    page.querySelector<HTMLInputElement>('.toggle input')!.click();
    await fixture.whenStable();

    expect(page.textContent).not.toContain('Dependants can join');
    expect(page.textContent).toContain('Annual premium');
  });

  it('explains when a policy is still being processed', async () => {
    const fixture = await render();
    for (const request of comparisonRequests()) {
      request.flush(null, { status: 409, statusText: 'Conflict' });
    }
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain(
      'still being processed',
    );
  });

  it('does not compare a policy with itself', async () => {
    const fixture = await render('a1', 'a1');

    expect(comparisonRequests()).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);
  });
});
