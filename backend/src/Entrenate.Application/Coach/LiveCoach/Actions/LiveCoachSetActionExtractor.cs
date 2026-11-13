using System.Globalization;
using System.Text.RegularExpressions;

namespace Entrenate.Application.Coach.LiveCoach.Actions
{
    public sealed record LiveCoachSetActionExtraction(
        int? Repetitions,
        decimal? WeightKg,
        int? Rir,
        bool IsValid,
        string? MissingField);

    public enum LiveCoachPendingSetPatchStatus
    {
        Valid,
        Ambiguous,
        Invalid
    }

    public sealed record LiveCoachPendingSetPatchExtraction(
        LiveCoachPendingSetPatchStatus Status,
        PendingLiveCoachSetPatch? Patch = null,
        string? InvalidField = null);

    public interface ILiveCoachSetActionExtractor
    {
        LiveCoachSetActionExtraction Extract(string text);
        LiveCoachPendingSetPatchExtraction ExtractPatch(string text);
    }

    public sealed partial class LiveCoachSetActionExtractor : ILiveCoachSetActionExtractor
    {
        private const string NumberToken = @"(-?\d+(?:[.,]\d+)?|cero|un|uno|una|dos|tres|cuatro|cinco|seis|siete|ocho|nueve|diez|once|doce|trece|catorce|quince|dieciseis|diecisiete|dieciocho|diecinueve|veinte|treinta|cuarenta|cincuenta|sesenta|setenta|ochenta|noventa|cien|ciento|doscientos)";

        public LiveCoachSetActionExtraction Extract(string text)
        {
            var normalized = Normalize(text);
            var repetitions = ExtractRepetitions(normalized);
            var weight = ExtractWeight(normalized);
            var rir = ExtractRir(normalized);

            if (!repetitions.HasValue)
                return new(null, weight, rir, false, "repetitions");

            if (!weight.HasValue)
                return new(repetitions, null, rir, false, "weightKg");

            var valid = repetitions is >= 1 and <= 200 &&
                weight is >= 0 and <= 1000 &&
                (!rir.HasValue || rir is >= 0 and <= 5);

            return new(repetitions, weight, rir, valid, valid ? null : "invalid");
        }

        public LiveCoachPendingSetPatchExtraction ExtractPatch(string text)
        {
            var normalized = Normalize(text);
            var weight = ExtractWeightPatch(normalized);
            var repetitions = ExtractRepetitionsPatch(normalized, weight.HasValue);
            var rir = ExtractRir(normalized);

            if (!weight.HasValue && !repetitions.HasValue && !rir.HasValue)
                return new(LiveCoachPendingSetPatchStatus.Ambiguous);

            if (weight.HasValue &&
                (weight.Value < 0 || weight.Value > 1000 || DecimalPlaces(weight.Value) > 2))
            {
                return new(LiveCoachPendingSetPatchStatus.Invalid, InvalidField: "weightKg");
            }

            if (repetitions.HasValue && (repetitions.Value < 1 || repetitions.Value > 200))
                return new(LiveCoachPendingSetPatchStatus.Invalid, InvalidField: "repetitions");

            if (rir.HasValue && (rir.Value < 0 || rir.Value > 5))
                return new(LiveCoachPendingSetPatchStatus.Invalid, InvalidField: "rir");

            return new(
                LiveCoachPendingSetPatchStatus.Valid,
                new PendingLiveCoachSetPatch(weight, repetitions, rir));
        }

        private static int? ExtractRepetitions(string text)
        {
            var match = RepetitionsWithUnitRegex().Match(text);
            if (match.Success) return ParseInteger(match.Groups[1].Value);

            match = RepetitionsAfterLabelRegex().Match(text);
            if (match.Success) return ParseInteger(match.Groups[1].Value);

            match = RepetitionsBeforeConWeightRegex().Match(text);
            if (match.Success) return ParseInteger(match.Groups[1].Value);

            match = RepetitionsAfterVerbRegex().Match(text);
            return match.Success ? ParseInteger(match.Groups[1].Value) : null;
        }

        private static decimal? ExtractWeight(string text)
        {
            if (text.Contains("sin peso", StringComparison.Ordinal) ||
                text.Contains("peso corporal", StringComparison.Ordinal))
                return 0m;

            var match = WeightWithUnitRegex().Match(text);
            if (match.Success) return ParseDecimal(match.Groups[1].Value);

            match = WeightBeforeLabelRegex().Match(text);
            if (match.Success) return ParseDecimal(match.Groups[1].Value);

            match = WeightAfterLabelRegex().Match(text);
            if (match.Success) return ParseDecimal(match.Groups[1].Value);

            match = WeightAfterConRegex().Match(text);
            return match.Success ? ParseDecimal(match.Groups[1].Value) : null;
        }

        private static int? ExtractRepetitionsPatch(string text, bool hasExplicitWeight)
        {
            var match = RepetitionsWithUnitRegex().Match(text);
            if (match.Success) return ParseInteger(match.Groups[1].Value);

            match = RepetitionsAfterLabelRegex().Match(text);
            if (match.Success) return ParseInteger(match.Groups[1].Value);

            match = HiceBareRepetitionsRegex().Match(text);
            if (match.Success) return ParseInteger(match.Groups[1].Value);

            if (hasExplicitWeight)
            {
                match = PendingRepetitionsAfterVerbRegex().Match(text);
                if (match.Success) return ParseInteger(match.Groups[1].Value);
            }

            return null;
        }

        private static decimal? ExtractWeightPatch(string text)
        {
            var weight = ExtractWeight(text);
            if (weight.HasValue) return weight;

            var match = WeightAfterLabelRegex().Match(text);
            return match.Success ? ParseDecimal(match.Groups[1].Value) : null;
        }

        private static int? ExtractRir(string text)
        {
            var match = RirRegex().Match(text);
            return match.Success ? ParseInteger(match.Groups[1].Value) : null;
        }

        private static int? ParseInteger(string value)
        {
            return SpanishNumberNormalizer.TryParseInteger(value, out var number)
                ? number
                : null;
        }

        private static decimal? ParseDecimal(string value)
        {
            var normalized = value.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                return number;

            return ParseInteger(value);
        }

        private static int DecimalPlaces(decimal value)
        {
            var bits = decimal.GetBits(value);
            return (bits[3] >> 16) & 0x7F;
        }

        private static string Normalize(string value)
            => SpanishNumberNormalizer.NormalizeText(value);

        [GeneratedRegex(NumberToken + @"\s*(?:reps?|repeticiones?)\b", RegexOptions.IgnoreCase)]
        private static partial Regex RepetitionsWithUnitRegex();

        [GeneratedRegex(@"\b(?:reps?|repeticiones?)\s*(?:a\s*)?" + NumberToken, RegexOptions.IgnoreCase)]
        private static partial Regex RepetitionsAfterLabelRegex();

        [GeneratedRegex(@"\bhice\s+" + NumberToken + @"\b", RegexOptions.IgnoreCase)]
        private static partial Regex HiceBareRepetitionsRegex();

        [GeneratedRegex(NumberToken + @"\s+con\s+" + NumberToken + @"(?:\s*(?:kg|kilos?))?\b", RegexOptions.IgnoreCase)]
        private static partial Regex RepetitionsBeforeConWeightRegex();

        [GeneratedRegex(@"(?:hice|fueron|termine|complete|anotame|anota|registra|registrame)\s+" + NumberToken, RegexOptions.IgnoreCase)]
        private static partial Regex RepetitionsAfterVerbRegex();

        [GeneratedRegex(@"(?:hice|fueron|eran)\s+" + NumberToken + @"\b(?!\s*(?:kg|kilos?)\b)", RegexOptions.IgnoreCase)]
        private static partial Regex PendingRepetitionsAfterVerbRegex();

        [GeneratedRegex(NumberToken + @"\s*(?:kg|kilos?)\b", RegexOptions.IgnoreCase)]
        private static partial Regex WeightWithUnitRegex();

        [GeneratedRegex(@"\bcon\s+" + NumberToken + @"(?!\s*(?:reps?|repeticiones?))", RegexOptions.IgnoreCase)]
        private static partial Regex WeightAfterConRegex();

        [GeneratedRegex(@"\bpeso\s*(?:a\s*)?" + NumberToken, RegexOptions.IgnoreCase)]
        private static partial Regex WeightAfterLabelRegex();

        [GeneratedRegex(NumberToken + @"\s+(?:en\s+)?peso\b", RegexOptions.IgnoreCase)]
        private static partial Regex WeightBeforeLabelRegex();

        [GeneratedRegex(@"\brir\s*(?:a\s*)?" + NumberToken, RegexOptions.IgnoreCase)]
        private static partial Regex RirRegex();
    }
}
