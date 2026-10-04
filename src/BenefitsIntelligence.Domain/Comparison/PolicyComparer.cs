using System.Globalization;
using System.Text.Json;

using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Domain.Comparison;

/// <summary>
/// Compares the structured facts of two policies. The result is calculated, never generated:
/// any written summary is produced from these differences rather than from the documents.
/// </summary>
public static class PolicyComparer
{
    private const string DateFormat = "yyyy-MM-dd";

    public static PolicyComparison Compare(BenefitPolicy current, BenefitPolicy proposed)
    {
        RequireExtraction(current);
        RequireExtraction(proposed);

        if (current.BenefitType != proposed.BenefitType)
        {
            throw new ArgumentException("Only policies of the same benefit type can be compared.", nameof(proposed));
        }

        List<FieldDifference> differences =
        [
            Text("provider", current.Provider, proposed.Provider, Review(current, proposed, PolicyField.Provider)),
            Text("scheme_name", current.SchemeName, proposed.SchemeName, Review(current, proposed, PolicyField.SchemeName)),
            Amount("annual_premium", current.AnnualPremium, proposed.AnnualPremium, Review(current, proposed, PolicyField.AnnualPremium)),
            Amount("annual_excess", current.AnnualExcess, proposed.AnnualExcess, Review(current, proposed, PolicyField.AnnualExcess)),
            Date("effective_date", current.EffectiveDate, proposed.EffectiveDate, Review(current, proposed, PolicyField.EffectiveDate)),
            Date("renewal_date", current.RenewalDate, proposed.RenewalDate, Review(current, proposed, PolicyField.RenewalDate)),
            Flag("dependants_allowed", current.DependantsAllowed, proposed.DependantsAllowed, Review(current, proposed, PolicyField.DependantsAllowed)),
            Flag("dependants_included", current.DependantsIncluded, proposed.DependantsIncluded, Review(current, proposed, PolicyField.DependantsIncluded)),
        ];

        foreach (CoverageType type in Enum.GetValues<CoverageType>())
        {
            differences.AddRange(CompareCoverage(
                type,
                current.CoverageItems.SingleOrDefault(c => c.Type == type),
                proposed.CoverageItems.SingleOrDefault(c => c.Type == type)));
        }

        foreach (EligibilityRuleType type in Enum.GetValues<EligibilityRuleType>())
        {
            differences.Add(CompareEligibility(
                type,
                current.EligibilityRules.SingleOrDefault(r => r.Type == type),
                proposed.EligibilityRules.SingleOrDefault(r => r.Type == type)));
        }

        return new PolicyComparison(current.Id, proposed.Id, differences);
    }

    private static IEnumerable<FieldDifference> CompareCoverage(CoverageType type, CoverageItem? current, CoverageItem? proposed)
    {
        string field = $"coverage.{Key(type)}";
        bool review = NeedsReview(current?.Assessment) || NeedsReview(proposed?.Assessment);

        yield return Flag($"{field}.covered", current?.Covered, proposed?.Covered, review);

        if (current?.SessionLimit is not null || proposed?.SessionLimit is not null)
        {
            yield return Count($"{field}.sessions", current?.SessionLimit, proposed?.SessionLimit, review);
        }

        yield return Text($"{field}.limit", current?.Limit, proposed?.Limit, review);
    }

    private static FieldDifference CompareEligibility(EligibilityRuleType type, EligibilityRule? current, EligibilityRule? proposed)
    {
        string field = $"eligibility.{Key(type)}";
        bool review = NeedsReview(current?.Assessment) || NeedsReview(proposed?.Assessment);

        if (type == EligibilityRuleType.MinimumServiceMonths)
        {
            return Count(field, ParseCount(current?.Value), ParseCount(proposed?.Value), review);
        }

        FieldDifference difference = Text(field, current?.Value, proposed?.Value, review);

        return (SetsNoRequirement(current), SetsNoRequirement(proposed)) switch
        {
            (true, true) => difference with { Change = Change.Unchanged },
            (false, true) when current?.Value is not null => difference with { Change = Change.Removed },
            (true, false) when proposed?.Value is not null => difference with { Change = Change.Added },
            _ => difference,
        };
    }

    // A rule with no value but a supporting quote is the document saying there is no requirement
    // (for example "not restricted by grade"), which is different from the document being silent.
    private static bool SetsNoRequirement(EligibilityRule? rule) =>
        rule is { Value: null, Assessment.Evidence: not null }
        && !rule.Assessment.ReviewReasons.HasFlag(ReviewReasons.Ambiguous);

    private static FieldDifference Amount(string field, decimal? current, decimal? proposed, bool review)
    {
        string? before = current?.ToString("0.00", CultureInfo.InvariantCulture);
        string? after = proposed?.ToString("0.00", CultureInfo.InvariantCulture);

        return Ordered(field, ValueKind.Money, before, after, current, proposed, review);
    }

    private static FieldDifference Count(string field, int? current, int? proposed, bool review)
    {
        string? before = current?.ToString(CultureInfo.InvariantCulture);
        string? after = proposed?.ToString(CultureInfo.InvariantCulture);

        return Ordered(field, ValueKind.Count, before, after, current, proposed, review);
    }

    private static FieldDifference Ordered(
        string field,
        ValueKind kind,
        string? currentText,
        string? proposedText,
        decimal? current,
        decimal? proposed,
        bool review)
    {
        if (current is not { } before || proposed is not { } after)
        {
            return new FieldDifference(field, kind, currentText, proposedText, null, Change.Unknown, review);
        }

        Change change = after.CompareTo(before) switch
        {
            > 0 => Change.Increased,
            < 0 => Change.Decreased,
            _ => Change.Unchanged,
        };

        return new FieldDifference(field, kind, currentText, proposedText, after - before, change, review);
    }

    private static FieldDifference Date(string field, DateOnly? current, DateOnly? proposed, bool review) =>
        Equatable(
            field,
            ValueKind.Date,
            current?.ToString(DateFormat, CultureInfo.InvariantCulture),
            proposed?.ToString(DateFormat, CultureInfo.InvariantCulture),
            review);

    private static FieldDifference Flag(string field, bool? current, bool? proposed, bool review) =>
        Equatable(field, ValueKind.Flag, FormatFlag(current), FormatFlag(proposed), review);

    // Providers word the same cover differently, so text is compared literally (ignoring case and
    // spacing) and reported as changed rather than interpreted.
    private static FieldDifference Text(string field, string? current, string? proposed, bool review) =>
        Equatable(field, ValueKind.Text, current, proposed, review);

    private static FieldDifference Equatable(string field, ValueKind kind, string? current, string? proposed, bool review)
    {
        Change change;
        if (current is null || proposed is null)
        {
            change = Change.Unknown;
        }
        else
        {
            change = string.Equals(Normalise(current), Normalise(proposed), StringComparison.OrdinalIgnoreCase)
                ? Change.Unchanged
                : Change.Changed;
        }

        return new FieldDifference(field, kind, current, proposed, null, change, review);
    }

    private static string? FormatFlag(bool? value) => value switch
    {
        true => "true",
        false => "false",
        null => null,
    };

    private static string Normalise(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static int? ParseCount(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int count) ? count : null;

    private static bool Review(BenefitPolicy current, BenefitPolicy proposed, PolicyField field) =>
        NeedsReview(current.FieldAssessments.SingleOrDefault(a => a.Field == field)?.Assessment)
        || NeedsReview(proposed.FieldAssessments.SingleOrDefault(a => a.Field == field)?.Assessment);

    private static bool NeedsReview(FactAssessment? assessment) => assessment?.NeedsReview ?? false;

    private static string Key<TEnum>(TEnum value)
        where TEnum : struct, Enum => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    private static void RequireExtraction(BenefitPolicy policy)
    {
        if (!policy.HasExtraction)
        {
            throw new PolicyNotExtractedException(policy.Id);
        }
    }
}
