import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PolicyAnswer } from '../policy.models';
import { AskPanel, SUGGESTED_QUESTIONS } from './ask-panel';

const ANSWER: PolicyAnswer = {
  answer: 'Yes. Physiotherapy is covered for up to 10 sessions in each scheme year.',
  supported: true,
  citations: [
    {
      pageStart: 4,
      pageEnd: 4,
      quote: 'Physiotherapy is covered for up to ten sessions per covered person',
    },
    { pageStart: 2, pageEnd: 3, quote: 'Physiotherapy Up to 10 sessions per scheme year' },
  ],
};

const REFUSED: PolicyAnswer = {
  answer: 'I cannot confirm this from the available policy information.',
  supported: false,
  citations: [],
};

describe('AskPanel', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<AskPanel>;
  let panel: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AskPanel);
    fixture.componentRef.setInput('policyId', 'b2c3');
    await fixture.whenStable();
    panel = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => http.verify());

  async function type(question: string) {
    const box = panel.querySelector<HTMLTextAreaElement>('textarea')!;
    box.value = question;
    box.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function submit() {
    panel.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('asks the API and shows the answer with the pages it cites', async () => {
    await type('  Is physiotherapy covered?  ');
    await submit();

    const request = http.expectOne('/api/policies/b2c3/questions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ question: 'Is physiotherapy covered?' });
    expect(panel.textContent).toContain('Reading the policy');

    request.flush(ANSWER);
    await fixture.whenStable();

    expect(panel.querySelector('.answer')?.textContent).toContain('up to 10 sessions');
    const sources = [...panel.querySelectorAll('.citations li')].map((li) => li.textContent);
    expect(sources[0]).toContain('Page 4');
    expect(sources[1]).toContain('Pages 2–3');
    expect(panel.querySelector('textarea')!.value).toBe('');
  });

  it('shows a refusal as a refusal, not as an answer', async () => {
    await type('Does it cover IVF?');
    await submit();
    http.expectOne('/api/policies/b2c3/questions').flush(REFUSED);
    await fixture.whenStable();

    expect(panel.querySelector('.answer')).toBeNull();
    expect(panel.querySelector('.refusal')?.textContent).toContain('Not stated in this policy');
    expect(panel.querySelector('.citations')).toBeNull();
  });

  it('asks a suggested question straight away', async () => {
    panel.querySelector<HTMLButtonElement>('.suggestion')!.click();
    await fixture.whenStable();

    const request = http.expectOne('/api/policies/b2c3/questions');
    expect(request.request.body).toEqual({ question: SUGGESTED_QUESTIONS[0] });
    request.flush(ANSWER);
  });

  it('does not ask an empty question or a second one while answering', async () => {
    const button = panel.querySelector<HTMLButtonElement>('button[type=submit]')!;
    expect(button.disabled).toBe(true);

    await type('Is physiotherapy covered?');
    await submit();
    await type('What is the excess?');

    expect(button.disabled).toBe(true);
    http.expectOne('/api/policies/b2c3/questions').flush(ANSWER);
  });

  it('keeps earlier questions, newest first', async () => {
    await type('First question?');
    await submit();
    http.expectOne('/api/policies/b2c3/questions').flush(ANSWER);
    await fixture.whenStable();

    await type('Second question?');
    await submit();
    http.expectOne('/api/policies/b2c3/questions').flush(REFUSED);
    await fixture.whenStable();

    const questions = [...panel.querySelectorAll('.question')].map((q) => q.textContent?.trim());
    expect(questions).toEqual(['Second question?', 'First question?']);
  });

  it('explains why a policy cannot be asked about', async () => {
    await type('Is physiotherapy covered?');
    await submit();
    http.expectOne('/api/policies/b2c3/questions').flush(
      {
        detail:
          'This policy was processed before its text was kept for questions. Upload it again to ask about it.',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();

    expect(panel.querySelector('.error')?.textContent).toContain('Upload it again');
  });

  it('reports when answers are unavailable', async () => {
    await type('Is physiotherapy covered?');
    await submit();
    http
      .expectOne('/api/policies/b2c3/questions')
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();

    expect(panel.querySelector('.error')?.textContent).toContain('cannot be answered right now');
  });
});
