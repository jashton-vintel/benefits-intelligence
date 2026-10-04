import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { REVIEW_REASON_LABELS } from '../policy-labels';
import { Evidence } from '../policy.models';
import { FactRow } from './fact-rows';

@Component({
  selector: 'app-fact-table',
  templateUrl: './fact-table.html',
  styleUrl: './fact-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FactTable {
  readonly caption = input.required<string>();
  readonly rows = input.required<FactRow[]>();

  protected readonly reasonLabels = REVIEW_REASON_LABELS;

  protected pages(evidence: Evidence): string {
    return evidence.pageStart === evidence.pageEnd
      ? `Page ${evidence.pageStart}`
      : `Pages ${evidence.pageStart}–${evidence.pageEnd}`;
  }
}
