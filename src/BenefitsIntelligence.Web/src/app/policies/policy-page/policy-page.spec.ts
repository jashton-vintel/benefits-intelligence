import { registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import localeEnGb from '@angular/common/locales/en-GB';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { PolicyDetail } from '../policy.models';
import { policyDetail } from '../testing/policy-data';
import { PolicyPage } from './policy-page';

describe('PolicyPage', () => {
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

  async function render(policy: PolicyDetail) {
    const fixture = TestBed.createComponent(PolicyPage);
    fixture.componentRef.setInput('id', policy.id);
    await fixture.whenStable();
    http.expectOne(`/api/policies/${policy.id}`).flush(policy);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function row(page: HTMLElement, label: string): HTMLElement {
    const header = [...page.querySelectorAll('th[scope=row]')].find(
      (cell) => cell.textContent?.trim() === label,
    );
    return header!.parentElement!;
  }

  it('shows each extracted value with its source page and quote', async () => {
    const page = await render(policyDetail());

    const excess = row(page, 'Annual excess');
    expect(excess.textContent).toContain('£150');
    expect(excess.textContent).toContain('95%');
    expect(excess.textContent).toContain('Page 3');
    expect(excess.querySelector('q')?.textContent).toBe('An excess of £150 applies');
  });

  it('highlights fields that need review and explains why', async () => {
    const page = await render(policyDetail());

    const dependants = row(page, 'Dependants paid for by employer');
    expect(dependants.classList).toContain('needs-review');
    expect(dependants.textContent).toContain('Unclear');
    expect(dependants.textContent).toContain('The document leaves this open or contradicts itself');
    expect(dependants.textContent).toContain('Pages 2–3');
    expect(page.querySelector('.alert.warning')?.textContent).toContain('1 field needs review');
  });

  it('shows why processing failed', async () => {
    const page = await render(
      policyDetail({
        status: 'Failed',
        failureCode: 'INVALID_DOCUMENT',
        failureMessage: 'The PDF is password protected.',
        extraction: null,
      }),
    );

    expect(page.querySelector('.alert.danger')?.textContent).toContain(
      'The PDF is password protected.',
    );
    expect(page.querySelector('app-fact-table')).toBeNull();
  });

  it('reports a policy that does not exist', async () => {
    const fixture = TestBed.createComponent(PolicyPage);
    fixture.componentRef.setInput('id', 'missing');
    await fixture.whenStable();
    http.expectOne('/api/policies/missing').flush(null, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('This policy does not exist.');
  });
});
