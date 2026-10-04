import { FieldDifference } from '../policy.models';
import { comparisonRow, comparisonSections, fieldLabel } from './comparison-rows';

function difference(overrides: Partial<FieldDifference> = {}): FieldDifference {
  return {
    field: 'annual_premium',
    kind: 'Money',
    current: '120000.00',
    proposed: '108000.00',
    delta: -12000,
    change: 'Decreased',
    needsReview: false,
    ...overrides,
  };
}

describe('comparison rows', () => {
  it('formats money and shows the size of the change', () => {
    const row = comparisonRow(difference(), 'en-GB');

    expect([row.current, row.proposed, row.change]).toEqual(['£120,000', '£108,000', '−£12,000']);
  });

  it('shows count changes with a sign', () => {
    const row = comparisonRow(
      difference({
        field: 'coverage.physiotherapy.sessions',
        kind: 'Count',
        current: '8',
        proposed: '10',
        delta: 2,
        change: 'Increased',
      }),
      'en-GB',
    );

    expect([row.label, row.change]).toEqual(['Physiotherapy: sessions per year', '+2']);
  });

  it('describes a requirement that has been removed', () => {
    const row = comparisonRow(
      difference({
        field: 'eligibility.minimum_grade',
        kind: 'Text',
        current: 'Grade 4',
        proposed: null,
        delta: null,
        change: 'Removed',
      }),
      'en-GB',
    );

    expect([row.label, row.current, row.proposed, row.change]).toEqual([
      'Minimum grade',
      'Grade 4',
      'No requirement',
      'Removed',
    ]);
  });

  it('marks values that cannot be compared', () => {
    const row = comparisonRow(
      difference({
        field: 'dependants_included',
        kind: 'Flag',
        current: 'true',
        proposed: null,
        delta: null,
        change: 'Unknown',
        needsReview: true,
      }),
      'en-GB',
    );

    expect([row.current, row.proposed, row.change, row.needsReview]).toEqual([
      'Yes',
      'Not stated',
      'Cannot compare',
      true,
    ]);
  });

  it('groups rows into policy, coverage and eligibility sections', () => {
    const sections = comparisonSections(
      [
        difference(),
        difference({ field: 'coverage.cancer.covered', kind: 'Flag', change: 'Unchanged' }),
        difference({ field: 'eligibility.country', kind: 'Text', change: 'Changed' }),
      ],
      'en-GB',
    );

    expect(sections.map((section) => [section.title, section.rows.length])).toEqual([
      ['Policy', 1],
      ['Coverage', 1],
      ['Eligibility', 1],
    ]);
  });

  it('labels fields it does not know from their path', () => {
    expect(fieldLabel('coverage.dental.covered')).toBe('Dental: covered');
    expect(fieldLabel('waiting_period')).toBe('Waiting period');
  });
});
