import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { Upload } from './upload';

describe('Upload', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render() {
    const fixture = TestBed.createComponent(Upload);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    const fill = async (name: string, file?: File) => {
      const nameInput = page.querySelector<HTMLInputElement>('#name')!;
      nameInput.value = name;
      nameInput.dispatchEvent(new Event('input'));

      if (file) {
        const fileInput = page.querySelector<HTMLInputElement>('#file')!;
        Object.defineProperty(fileInput, 'files', { value: { item: () => file } });
        fileInput.dispatchEvent(new Event('change'));
      }
      await fixture.whenStable();
    };

    const submit = async () => {
      page.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };

    return { fixture, page, fill, submit };
  }

  const pdf = () => new File(['%PDF-1.7'], 'policy.pdf', { type: 'application/pdf' });

  it('uploads the policy and opens it', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const { fill, submit } = await render();

    await fill('  Proposed policy  ', pdf());
    await submit();

    const request = http.expectOne('/api/policies');
    expect(request.request.method).toBe('POST');
    const body = request.request.body as FormData;
    expect(body.get('name')).toBe('Proposed policy');
    expect((body.get('file') as File).name).toBe('policy.pdf');

    request.flush({ policyId: 'b2c3', correlationId: 'c4d5' });
    expect(navigate).toHaveBeenCalledWith(['/policies', 'b2c3']);
  });

  it('requires a name and a document before uploading', async () => {
    const { page, submit } = await render();

    await submit();

    expect(page.textContent).toContain('Enter a name for the policy.');
    expect(page.textContent).toContain('Choose a PDF document to upload.');
    http.expectNone('/api/policies');
  });

  it('rejects files that are not PDFs', async () => {
    const { page, fill, submit } = await render();

    await fill('Proposed policy', new File(['text'], 'notes.txt', { type: 'text/plain' }));
    await submit();

    expect(page.textContent).toContain('The document must be a PDF.');
    http.expectNone('/api/policies');
  });

  it('shows validation errors returned by the API', async () => {
    const { fixture, page, fill, submit } = await render();
    await fill('Proposed policy', pdf());
    await submit();

    http
      .expectOne('/api/policies')
      .flush(
        { errors: { file: ['The file is not a PDF document.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    expect(page.textContent).toContain('The file is not a PDF document.');
  });
});
