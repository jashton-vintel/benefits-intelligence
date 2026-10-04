import {
  BenefitType,
  CoverageType,
  EligibilityRuleType,
  ProcessingStatus,
  ReviewReason,
} from './policy.models';

export const BENEFIT_TYPE_LABELS: Record<BenefitType, string> = {
  PrivateMedicalInsurance: 'Private medical insurance',
};

export const STATUS_LABELS: Record<ProcessingStatus, string> = {
  Uploaded: 'Uploaded',
  Queued: 'Processing',
  Completed: 'Completed',
  Failed: 'Failed',
};

export const COVERAGE_LABELS: Record<CoverageType, string> = {
  Inpatient: 'In-patient and day-patient',
  Outpatient: 'Out-patient',
  Diagnostics: 'Diagnostics',
  Physiotherapy: 'Physiotherapy',
  MentalHealth: 'Mental health',
  Cancer: 'Cancer',
};

export const ELIGIBILITY_LABELS: Record<EligibilityRuleType, string> = {
  EmploymentType: 'Employment type',
  Country: 'Country',
  MinimumServiceMonths: 'Minimum service',
  MinimumGrade: 'Minimum grade',
};

export const REVIEW_REASON_LABELS: Record<ReviewReason, string> = {
  LowConfidence: 'Low confidence',
  Ambiguous: 'The document leaves this open or contradicts itself',
  EvidenceMissing: 'No supporting quote was given',
  EvidenceNotFound: 'The quoted text was not found in the document',
  ValueNotInEvidence: 'The value does not appear in the quoted text',
};
