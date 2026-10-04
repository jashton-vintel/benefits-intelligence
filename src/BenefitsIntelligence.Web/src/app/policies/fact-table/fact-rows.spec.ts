import { policyDetail } from '../testing/policy-data';
import { coverageRows, eligibilityRows, headerRows, NOT_STATED, UNCLEAR } from './fact-rows';

describe('fact rows', () => {
  const extraction = policyDetail().extraction!;

  it('formats header values for display', () => {
    const rows = headerRows(extraction, 'en-GB');
    const value = (label: string) => rows.find((row) => row.label === label)?.value;

    expect(value('Annual premium')).toBe('£108,000');
    expect(value('Annual excess')).toBe('£150');
    expect(value('Effective date')).toBe('1 April 2027');
    expect(value('Dependants can join')).toBe('Yes');
  });

  it('distinguishes an ambiguous value from one the document does not state', () => {
    const rows = headerRows(
      {
        ...extraction,
        renewalDate: {
          value: null,
          assessment: { ...extraction.renewalDate.assessment, evidence: null },
        },
      },
      'en-GB',
    );
    const value = (label: string) => rows.find((row) => row.label === label)?.value;

    expect(value('Dependants paid for by employer')).toBe(UNCLEAR);
    expect(value('Renewal date')).toBe(NOT_STATED);
  });

  it('describes coverage by its limit', () => {
    expect(coverageRows(extraction.coverage)).toEqual([
      expect.objectContaining({
        label: 'Physiotherapy',
        value: 'Up to 10 sessions per scheme year',
      }),
    ]);
  });

  it('reports cover the document excludes', () => {
    const [row] = coverageRows([{ ...extraction.coverage[0], covered: false }]);

    expect(row.value).toBe('Not covered');
  });

  it('describes eligibility rules in plain terms', () => {
    const values = eligibilityRows(extraction.eligibility).map((row) => row.value);

    // A quoted null minimum grade means the document sets no requirement.
    expect(values).toEqual(['None', 'No requirement']);
  });

  it('shows service requirements in months', () => {
    const [row] = eligibilityRows([{ ...extraction.eligibility[0], value: '3' }]);

    expect(row.value).toBe('3 months');
  });
});
