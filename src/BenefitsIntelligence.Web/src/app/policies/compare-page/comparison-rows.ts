import { formatCurrency, formatDate, formatNumber } from '@angular/common';

import { COVERAGE_LABELS, ELIGIBILITY_LABELS } from '../policy-labels';
import { CoverageType, EligibilityRuleType, FieldDifference } from '../policy.models';

export interface ComparisonRow {
  field: string;
  label: string;
  current: string;
  proposed: string;
  change: string;
  unchanged: boolean;
  needsReview: boolean;
}

export interface ComparisonSection {
  title: string;
  rows: ComparisonRow[];
}

const HEADER_LABELS: Record<string, string> = {
  provider: 'Provider',
  scheme_name: 'Scheme',
  annual_premium: 'Annual premium',
  annual_excess: 'Annual excess',
  effective_date: 'Effective date',
  renewal_date: 'Renewal date',
  dependants_allowed: 'Dependants can join',
  dependants_included: 'Dependants paid for by employer',
};

const COVERAGE_KEYS: Record<string, CoverageType> = {
  inpatient: 'Inpatient',
  outpatient: 'Outpatient',
  diagnostics: 'Diagnostics',
  physiotherapy: 'Physiotherapy',
  mental_health: 'MentalHealth',
  cancer: 'Cancer',
};

const COVERAGE_PARTS: Record<string, string> = {
  covered: 'covered',
  sessions: 'sessions per year',
  limit: 'terms',
};

const ELIGIBILITY_KEYS: Record<string, EligibilityRuleType> = {
  employment_type: 'EmploymentType',
  country: 'Country',
  minimum_service_months: 'MinimumServiceMonths',
  minimum_grade: 'MinimumGrade',
};

export function comparisonSections(
  differences: FieldDifference[],
  locale: string,
): ComparisonSection[] {
  const rows = differences.map((difference) => comparisonRow(difference, locale));
  const section = (title: string, prefix: string | null) => ({
    title,
    rows: rows.filter((row) =>
      prefix === null ? !row.field.includes('.') : row.field.startsWith(prefix),
    ),
  });

  return [
    section('Policy', null),
    section('Coverage', 'coverage.'),
    section('Eligibility', 'eligibility.'),
  ].filter((group) => group.rows.length > 0);
}

export function comparisonRow(difference: FieldDifference, locale: string): ComparisonRow {
  return {
    field: difference.field,
    label: fieldLabel(difference.field),
    current: formatValue(difference, difference.current, 'current', locale),
    proposed: formatValue(difference, difference.proposed, 'proposed', locale),
    change: describeChange(difference, locale),
    unchanged: difference.change === 'Unchanged',
    needsReview: difference.needsReview,
  };
}

export function fieldLabel(field: string): string {
  const [group, key, part] = field.split('.');
  if (group === 'coverage') {
    const cover = COVERAGE_KEYS[key] ? COVERAGE_LABELS[COVERAGE_KEYS[key]] : humanise(key);
    return `${cover}: ${COVERAGE_PARTS[part] ?? humanise(part)}`;
  }
  if (group === 'eligibility') {
    return ELIGIBILITY_KEYS[key] ? ELIGIBILITY_LABELS[ELIGIBILITY_KEYS[key]] : humanise(key);
  }
  return HEADER_LABELS[field] ?? humanise(field);
}

function formatValue(
  difference: FieldDifference,
  value: string | null,
  side: 'current' | 'proposed',
  locale: string,
): string {
  if (value === null) {
    const noRequirement =
      (difference.change === 'Removed' && side === 'proposed') ||
      (difference.change === 'Added' && side === 'current');
    return noRequirement ? 'No requirement' : 'Not stated';
  }

  switch (difference.kind) {
    case 'Money':
      return money(Number(value), locale);
    case 'Date':
      return formatDate(value, 'd MMMM y', locale);
    case 'Flag':
      return value === 'true' ? 'Yes' : 'No';
    default:
      return value;
  }
}

function describeChange(difference: FieldDifference, locale: string): string {
  switch (difference.change) {
    case 'Unchanged':
      return 'No change';
    case 'Increased':
    case 'Decreased': {
      if (difference.delta === null) {
        return difference.change;
      }
      const sign = difference.delta > 0 ? '+' : '−';
      const size = Math.abs(difference.delta);
      return difference.kind === 'Money'
        ? `${sign}${money(size, locale)}`
        : `${sign}${formatNumber(size, locale, '1.0-0')}`;
    }
    case 'Unknown':
      return 'Cannot compare';
    default:
      return difference.change;
  }
}

function money(amount: number, locale: string): string {
  return formatCurrency(amount, locale, '£', 'GBP', '1.0-2');
}

function humanise(key: string | undefined): string {
  const text = (key ?? '').replaceAll('_', ' ');
  return text.charAt(0).toUpperCase() + text.slice(1);
}
