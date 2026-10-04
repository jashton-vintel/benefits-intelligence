import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, EMPTY } from 'rxjs';

import { pollWhile } from '../../shared/polling';
import { PolicyApi } from '../policy-api';
import { isInFlight } from '../policy.models';
import { StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DatePipe, RouterLink, StatusBadge],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly api = inject(PolicyApi);

  protected readonly loadFailed = signal(false);

  protected readonly policies = toSignal(
    pollWhile(
      () => this.api.list(),
      (policies) => policies.some((policy) => isInFlight(policy.status)),
    ).pipe(
      catchError(() => {
        this.loadFailed.set(true);
        return EMPTY;
      }),
    ),
  );

  protected readonly counts = computed(() => {
    const policies = this.policies() ?? [];
    return {
      total: policies.length,
      processing: policies.filter((policy) => isInFlight(policy.status)).length,
      needsReview: policies.filter((policy) => policy.status === 'Completed' && policy.needsReview)
        .length,
    };
  });
}
