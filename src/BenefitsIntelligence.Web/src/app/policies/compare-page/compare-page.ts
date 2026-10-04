import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  LOCALE_ID,
  signal,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, Observable, of, startWith, switchMap } from 'rxjs';

import { PolicyApi } from '../policy-api';
import { ComparisonSection, comparisonSections } from './comparison-rows';

type Loadable<T> =
  | { state: 'idle' }
  | { state: 'loading' }
  | { state: 'loaded'; value: T }
  | { state: 'failed'; message: string };

const IDLE = { state: 'idle' } as const;
const LOADING = { state: 'loading' } as const;

@Component({
  selector: 'app-compare-page',
  imports: [RouterLink],
  templateUrl: './compare-page.html',
  styleUrl: './compare-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComparePage {
  private readonly api = inject(PolicyApi);
  private readonly router = inject(Router);
  private readonly locale = inject(LOCALE_ID);

  /** Bound from the query string, so a comparison can be linked to. */
  readonly current = input<string>();
  readonly proposed = input<string>();

  protected readonly policies = toSignal(
    this.api.list().pipe(
      map((policies) => policies.filter((policy) => policy.status === 'Completed')),
      catchError(() => of([])),
    ),
  );

  protected readonly currentChoice = linkedSignal(() => this.current() ?? '');
  protected readonly proposedChoice = linkedSignal(() => this.proposed() ?? '');
  protected readonly showUnchanged = signal(true);

  private readonly selection = computed(() => {
    const current = this.current();
    const proposed = this.proposed();
    return current && proposed && current !== proposed ? { current, proposed } : null;
  });

  // The calculated comparison and the written summary are requested separately, so the table
  // appears straight away and the summary follows when the model has written it.
  protected readonly report = this.load((selection) =>
    this.api.compare(selection.current, selection.proposed),
  );

  protected readonly summary = this.load((selection) =>
    this.api
      .compare(selection.current, selection.proposed, true)
      .pipe(map((report) => report.summary)),
  );

  protected readonly sections = computed<ComparisonSection[]>(() => {
    const report = this.report();
    return report.state === 'loaded'
      ? comparisonSections(report.value.differences, this.locale)
      : [];
  });

  protected readonly reviewCount = computed(
    () =>
      this.sections()
        .flatMap((section) => section.rows)
        .filter((row) => row.needsReview).length,
  );

  protected readonly canCompare = computed(
    () =>
      this.currentChoice() !== '' &&
      this.proposedChoice() !== '' &&
      this.currentChoice() !== this.proposedChoice(),
  );

  protected compare(): void {
    this.router.navigate([], {
      queryParams: { current: this.currentChoice(), proposed: this.proposedChoice() },
    });
  }

  protected swap(): void {
    const current = this.currentChoice();
    this.currentChoice.set(this.proposedChoice());
    this.proposedChoice.set(current);
  }

  protected visibleRows(section: ComparisonSection) {
    return this.showUnchanged() ? section.rows : section.rows.filter((row) => !row.unchanged);
  }

  protected chosen(event: Event): string {
    return (event.target as HTMLSelectElement).value;
  }

  private load<T>(request: (selection: { current: string; proposed: string }) => Observable<T>) {
    return toSignal(
      toObservable(this.selection).pipe(
        switchMap((selection) =>
          selection === null
            ? of(IDLE)
            : request(selection).pipe(
                map((value): Loadable<T> => ({ state: 'loaded', value })),
                catchError((error: HttpErrorResponse) =>
                  of<Loadable<T>>({ state: 'failed', message: failureMessage(error) }),
                ),
                startWith(LOADING),
              ),
        ),
      ),
      { initialValue: IDLE as Loadable<T> },
    );
  }
}

function failureMessage(error: HttpErrorResponse): string {
  switch (error.status) {
    case 404:
      return 'One of these policies no longer exists.';
    case 409:
      return 'One of these policies is still being processed. Try again when it has finished.';
    default:
      return 'The comparison could not be loaded. Check that the API is running.';
  }
}
