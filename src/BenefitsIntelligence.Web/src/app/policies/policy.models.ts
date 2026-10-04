export type ProcessingStatus = 'Uploaded' | 'Queued' | 'Completed' | 'Failed';

export type BenefitType = 'PrivateMedicalInsurance';

export type ReviewReason =
  'LowConfidence' | 'Ambiguous' | 'EvidenceMissing' | 'EvidenceNotFound' | 'ValueNotInEvidence';

export type CoverageType =
  'Inpatient' | 'Outpatient' | 'Diagnostics' | 'Physiotherapy' | 'MentalHealth' | 'Cancer';

export type EligibilityRuleType =
  'EmploymentType' | 'Country' | 'MinimumServiceMonths' | 'MinimumGrade';

export interface PolicySummary {
  id: string;
  name: string;
  benefitType: BenefitType;
  fileName: string;
  createdAt: string;
  provider: string | null;
  schemeName: string | null;
  annualExcess: number | null;
  needsReview: boolean;
  status: ProcessingStatus;
  failureCode: string | null;
  failureMessage: string | null;
  statusUpdatedAt: string;
}

export interface Evidence {
  pageStart: number;
  pageEnd: number;
  quote: string;
}

export interface Assessment {
  confidence: number;
  needsReview: boolean;
  reviewReasons: ReviewReason[];
  evidence: Evidence | null;
}

export interface Fact<T> {
  value: T | null;
  assessment: Assessment;
}

export interface CoverageDetail {
  type: CoverageType;
  covered: boolean | null;
  limit: string | null;
  sessionLimit: number | null;
  assessment: Assessment;
}

export interface EligibilityDetail {
  type: EligibilityRuleType;
  value: string | null;
  assessment: Assessment;
}

export interface ExtractionDetail {
  needsReview: boolean;
  provider: Fact<string>;
  schemeName: Fact<string>;
  annualPremium: Fact<number>;
  annualExcess: Fact<number>;
  effectiveDate: Fact<string>;
  renewalDate: Fact<string>;
  dependantsAllowed: Fact<boolean>;
  dependantsIncluded: Fact<boolean>;
  coverage: CoverageDetail[];
  eligibility: EligibilityDetail[];
}

export interface PolicyDetail {
  id: string;
  name: string;
  benefitType: BenefitType;
  fileName: string;
  createdAt: string;
  status: ProcessingStatus;
  failureCode: string | null;
  failureMessage: string | null;
  statusUpdatedAt: string;
  extraction: ExtractionDetail | null;
}

export interface UploadResult {
  policyId: string;
  correlationId: string;
}

export function isInFlight(status: ProcessingStatus): boolean {
  return status === 'Uploaded' || status === 'Queued';
}
