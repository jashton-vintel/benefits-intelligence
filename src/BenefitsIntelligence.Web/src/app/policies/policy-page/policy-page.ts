import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  LOCALE_ID,
  signal,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, EMPTY, switchMap } from 'rxjs';

import { pollWhile } from '../../shared/polling';
import { AskPanel } from '../ask-panel/ask-panel';
import { coverageRows, eligibilityRows, headerRows } from '../fact-table/fact-rows';
import { FactTable } from '../fact-table/fact-table';
import { BENEFIT_TYPE_LABELS } from '../policy-labels';
import { PolicyApi } from '../policy-api';
import { isInFlight } from '../policy.models';
import { StatusBadge } from '../status-badge/status-badge';

const KEY_FIGURES = ['Provider', 'Scheme', 'Annual premium', 'Annual excess'];

@Component({
  selector: 'app-policy-page',
  imports: [AskPanel, DatePipe, FactTable, RouterLink, StatusBadge],
  templateUrl: './policy-page.html',
  styleUrl: './policy-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PolicyPage {
  private readonly api = inject(PolicyApi);
  private readonly locale = inject(LOCALE_ID);

  /** Bound from the route parameter. */
  readonly id = input.required<string>();

  protected readonly benefitTypeLabels = BENEFIT_TYPE_LABELS;
  protected readonly loadError = signal<string | null>(null);

  protected readonly policy = toSignal(
    toObservable(this.id).pipe(
      switchMap((id) =>
        pollWhile(
          () => this.api.get(id),
          (policy) => isInFlight(policy.status),
        ).pipe(
          catchError((error: HttpErrorResponse) => {
            this.loadError.set(
              error.status === 404
                ? 'This policy does not exist.'
                : 'The policy could not be loaded.',
            );
            return EMPTY;
          }),
        ),
      ),
    ),
  );

  private readonly extraction = computed(() => this.policy()?.extraction ?? null);

  protected readonly headerRows = computed(() => {
    const extraction = this.extraction();
    return extraction ? headerRows(extraction, this.locale) : [];
  });

  protected readonly keyFigures = computed(() =>
    this.headerRows().filter((row) => KEY_FIGURES.includes(row.label)),
  );

  protected readonly coverageRows = computed(() => coverageRows(this.extraction()?.coverage ?? []));

  protected readonly eligibilityRows = computed(() =>
    eligibilityRows(this.extraction()?.eligibility ?? []),
  );

  protected readonly reviewCount = computed(
    () =>
      [...this.headerRows(), ...this.coverageRows(), ...this.eligibilityRows()].filter(
        (row) => row.assessment.needsReview,
      ).length,
  );

  protected readonly inFlight = computed(() => {
    const policy = this.policy();
    return policy ? isInFlight(policy.status) : false;
  });
}
