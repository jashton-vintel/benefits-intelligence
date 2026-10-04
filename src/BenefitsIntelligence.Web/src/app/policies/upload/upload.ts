import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { formatFileSize } from '../../shared/file-size';
import { BENEFIT_TYPE_LABELS } from '../policy-labels';
import { PolicyApi } from '../policy-api';

export const MAX_NAME_LENGTH = 200;
export const MAX_DOCUMENT_BYTES = 20 * 1024 * 1024;

@Component({
  selector: 'app-upload',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './upload.html',
  styleUrl: './upload.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Upload {
  private readonly api = inject(PolicyApi);
  private readonly router = inject(Router);

  protected readonly maxNameLength = MAX_NAME_LENGTH;
  protected readonly formatFileSize = formatFileSize;
  protected readonly benefitTypeLabel = BENEFIT_TYPE_LABELS.PrivateMedicalInsurance;

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(MAX_NAME_LENGTH)],
    }),
  });

  protected readonly file = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly serverErrors = signal<string[]>([]);
  protected readonly submitting = signal(false);
  protected readonly dragging = signal(false);

  protected selectFile(event: Event): void {
    this.chooseFile((event.target as HTMLInputElement).files?.item(0) ?? null);
  }

  protected dragOver(event: DragEvent): void {
    // Without preventDefault the browser opens a dropped file instead of handing it to the page.
    event.preventDefault();
    this.dragging.set(true);
  }

  protected drop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    this.chooseFile(event.dataTransfer?.files.item(0) ?? null);
  }

  private chooseFile(file: File | null): void {
    this.file.set(file);
    this.fileError.set(file ? fileProblem(file) : null);
  }

  protected submit(): void {
    const file = this.file();
    if (!file) {
      this.fileError.set('Choose a PDF document to upload.');
    }
    if (this.form.invalid || !file || this.fileError()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.serverErrors.set([]);

    this.api.upload(this.form.controls.name.value.trim(), file).subscribe({
      next: (result) => this.router.navigate(['/policies', result.policyId]),
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        this.serverErrors.set(problemMessages(error));
      },
    });
  }
}

function fileProblem(file: File): string | null {
  if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
    return 'The document must be a PDF.';
  }
  if (file.size > MAX_DOCUMENT_BYTES) {
    return `The document must be no larger than ${formatFileSize(MAX_DOCUMENT_BYTES)}.`;
  }
  return null;
}

function problemMessages(error: HttpErrorResponse): string[] {
  // Validation failures come back as RFC 7807 problem details with errors keyed by field.
  const errors: unknown = error.error?.errors;
  if (errors && typeof errors === 'object') {
    return Object.values(errors as Record<string, string[]>).flat();
  }
  return ['The policy could not be uploaded. Please try again.'];
}
