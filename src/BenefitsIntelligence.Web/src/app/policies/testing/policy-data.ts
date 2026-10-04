import { Assessment, Fact, PolicyDetail, PolicySummary } from '../policy.models';

export function assessment(overrides: Partial<Assessment> = {}): Assessment {
  return {
    confidence: 0.95,
    needsReview: false,
    reviewReasons: [],
    evidence: { pageStart: 3, pageEnd: 3, quote: 'An excess of £150 applies' },
    ...overrides,
  };
}

function fact<T>(value: T | null, overrides: Partial<Assessment> = {}): Fact<T> {
  return { value, assessment: assessment(overrides) };
}

export function policySummary(overrides: Partial<PolicySummary> = {}): PolicySummary {
  return {
    id: 'b2c3',
    name: 'Proposed policy',
    benefitType: 'PrivateMedicalInsurance',
    fileName: 'ProposedHealthPolicy.pdf',
    createdAt: '2026-10-04T12:00:00Z',
    provider: 'NorthStar Health',
    schemeName: 'Essentials Select',
    annualExcess: 150,
    needsReview: false,
    status: 'Completed',
    failureCode: null,
    failureMessage: null,
    statusUpdatedAt: '2026-10-04T12:01:00Z',
    ...overrides,
  };
}

export function policyDetail(overrides: Partial<PolicyDetail> = {}): PolicyDetail {
  const ambiguous = {
    needsReview: true,
    reviewReasons: ['Ambiguous' as const],
    evidence: { pageStart: 2, pageEnd: 3, quote: 'will be confirmed at renewal' },
  };

  return {
    id: 'b2c3',
    name: 'Proposed policy',
    benefitType: 'PrivateMedicalInsurance',
    fileName: 'ProposedHealthPolicy.pdf',
    createdAt: '2026-10-04T12:00:00Z',
    status: 'Completed',
    failureCode: null,
    failureMessage: null,
    statusUpdatedAt: '2026-10-04T12:01:00Z',
    extraction: {
      needsReview: true,
      provider: fact('NorthStar Health'),
      schemeName: fact('Essentials Select'),
      annualPremium: fact(108000),
      annualExcess: fact(150),
      effectiveDate: fact('2027-04-01'),
      renewalDate: fact('2028-04-01'),
      dependantsAllowed: fact(true),
      dependantsIncluded: fact<boolean>(null, ambiguous),
      coverage: [
        {
          type: 'Physiotherapy',
          covered: true,
          limit: 'Up to 10 sessions per scheme year',
          sessionLimit: 10,
          assessment: assessment(),
        },
      ],
      eligibility: [
        { type: 'MinimumServiceMonths', value: '0', assessment: assessment() },
        { type: 'MinimumGrade', value: null, assessment: assessment() },
      ],
    },
    ...overrides,
  };
}
