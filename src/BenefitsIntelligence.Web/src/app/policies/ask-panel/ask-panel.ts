import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';

import { PolicyApi } from '../policy-api';
import { AnswerCitation, PolicyAnswer } from '../policy.models';

export const MAX_QUESTION_LENGTH = 500;

export const SUGGESTED_QUESTIONS = [
  'Is physiotherapy covered?',
  'What is the excess?',
  'Can dependants be added?',
  'Who is eligible to join?',
];

interface Exchange {
  id: number;
  question: string;
  state: 'asking' | 'answered' | 'failed';
  answer?: PolicyAnswer;
  error?: string;
}

@Component({
  selector: 'app-ask-panel',
  templateUrl: './ask-panel.html',
  styleUrl: './ask-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AskPanel {
  private readonly api = inject(PolicyApi);
  private nextId = 1;

  readonly policyId = input.required<string>();

  protected readonly maxLength = MAX_QUESTION_LENGTH;
  protected readonly suggestions = SUGGESTED_QUESTIONS;
  protected readonly draft = signal('');
  protected readonly exchanges = signal<Exchange[]>([]);

  protected readonly asking = computed(() =>
    this.exchanges().some((exchange) => exchange.state === 'asking'),
  );

  protected readonly canAsk = computed(() => {
    const question = this.draft().trim();
    return question.length > 0 && question.length <= MAX_QUESTION_LENGTH && !this.asking();
  });

  protected ask(question = this.draft()): void {
    const text = question.trim();
    if (!text || this.asking()) {
      return;
    }

    const id = this.nextId++;
    this.exchanges.update((list) => [{ id, question: text, state: 'asking' }, ...list]);
    this.draft.set('');

    this.api.ask(this.policyId(), text).subscribe({
      next: (answer) => this.settle(id, { state: 'answered', answer }),
      error: (error: HttpErrorResponse) =>
        this.settle(id, { state: 'failed', error: failureMessage(error) }),
    });
  }

  protected typed(event: Event): void {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  protected submitOnEnter(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.ask();
    }
  }

  protected pages(citation: AnswerCitation): string {
    return citation.pageStart === citation.pageEnd
      ? `Page ${citation.pageStart}`
      : `Pages ${citation.pageStart}–${citation.pageEnd}`;
  }

  private settle(id: number, changes: Partial<Exchange>): void {
    this.exchanges.update((list) =>
      list.map((exchange) => (exchange.id === id ? { ...exchange, ...changes } : exchange)),
    );
  }
}

function failureMessage(error: HttpErrorResponse): string {
  const detail: unknown = error.error?.detail;
  if (error.status === 409 && typeof detail === 'string') {
    return detail;
  }
  if (error.status === 400) {
    return 'Enter a question of up to 500 characters.';
  }
  return 'Questions cannot be answered right now. Please try again shortly.';
}
