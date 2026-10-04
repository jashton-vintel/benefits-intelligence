import { formatCurrency, formatDate } from '@angular/common';

import { COVERAGE_LABELS, ELIGIBILITY_LABELS } from '../policy-labels';
import {
  Assessment,
  CoverageDetail,
  EligibilityDetail,
  ExtractionDetail,
  Fact,
} from '../policy.models';

export interface FactRow {
  label: string;
  value: string;
  assessment: Assessment;
}

export const NOT_STATED = 'Not stated';
export const UNCLEAR = 'Unclear';

export function headerRows(extraction: ExtractionDetail, locale: string): FactRow[] {
  const money = (value: number) => formatCurrency(value, locale, '£', 'GBP', '1.0-2');
  const date = (value: string) => formatDate(value, 'd MMMM y', locale);

  return [
    row('Provider', extraction.provider, text),
    row('Scheme', extraction.schemeName, text),
    row('Annual premium', extraction.annualPremium, money),
    row('Annual excess', extraction.annualExcess, money),
    row('Effective date', extraction.effectiveDate, date),
    row('Renewal date', extraction.renewalDate, date),
    row('Dependants can join', extraction.dependantsAllowed, yesNo),
    row('Dependants paid for by employer', extraction.dependantsIncluded, yesNo),
  ];
}

export function coverageRows(coverage: CoverageDetail[]): FactRow[] {
  return coverage.map((item) => ({
    label: COVERAGE_LABELS[item.type],
    value: describeCoverage(item),
    assessment: item.assessment,
  }));
}

export function eligibilityRows(rules: EligibilityDetail[]): FactRow[] {
  return rules.map((rule) => ({
    label: ELIGIBILITY_LABELS[rule.type],
    value: describeEligibility(rule),
    assessment: rule.assessment,
  }));
}

function row<T>(label: string, fact: Fact<T>, format: (value: T) => string): FactRow {
  return {
    label,
    value: fact.value === null ? missingValue(fact.assessment) : format(fact.value),
    assessment: fact.assessment,
  };
}

// An empty value from an ambiguous clause is not the same as the document being silent.
function missingValue(assessment: Assessment): string {
  return assessment.reviewReasons.includes('Ambiguous') ? UNCLEAR : NOT_STATED;
}

function describeCoverage(item: CoverageDetail): string {
  if (item.covered === null) {
    return missingValue(item.assessment);
  }
  if (!item.covered) {
    return 'Not covered';
  }
  return item.limit ?? 'Covered';
}

function describeEligibility(rule: EligibilityDetail): string {
  if (rule.value === null) {
    // With a quote, the document says there is no requirement; without one it is silent.
    if (rule.assessment.reviewReasons.includes('Ambiguous')) {
      return UNCLEAR;
    }
    return rule.assessment.evidence ? 'No requirement' : NOT_STATED;
  }
  if (rule.type === 'MinimumServiceMonths') {
    const months = Number(rule.value);
    return months === 0 ? 'None' : `${months} ${months === 1 ? 'month' : 'months'}`;
  }
  return rule.value;
}

const text = (value: string) => value;
const yesNo = (value: boolean) => (value ? 'Yes' : 'No');
