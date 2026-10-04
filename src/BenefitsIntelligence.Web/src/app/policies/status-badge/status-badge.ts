import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { STATUS_LABELS } from '../policy-labels';
import { ProcessingStatus } from '../policy.models';

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="tone()">{{ label() }}</span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<ProcessingStatus>();
  readonly needsReview = input(false);

  protected readonly label = computed(() =>
    this.status() === 'Completed' && this.needsReview()
      ? 'Needs review'
      : STATUS_LABELS[this.status()],
  );

  protected readonly tone = computed(() => {
    switch (this.status()) {
      case 'Failed':
        return 'danger';
      case 'Completed':
        return this.needsReview() ? 'warning' : 'success';
      default:
        return 'info';
    }
  });
}
