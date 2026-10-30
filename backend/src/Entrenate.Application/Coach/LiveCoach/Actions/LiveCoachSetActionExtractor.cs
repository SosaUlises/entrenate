using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Entrenate.Application.Coach.LiveCoach.Actions
{
    public sealed record LiveCoachSetActionExtraction(
        int? Repetitions,
        decimal? WeightKg,
        int? Rir,
        bool IsValid,
        string? MissingField);

    public interface ILiveCoachSetActionExtractor
    {
        LiveCoachSetActionExtraction Extract(string text);
    }

    public sealed partial class LiveCoachSetActionExtractor : ILiveCoachSetActionExtractor
    {
        private static readonly IReadOnlyDictionary<string, int> NumberWords =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["cero"] = 0, ["un"] = 1, ["uno"] = 1, ["una"] = 1,
                ["dos"] = 2, ["tres"] = 3, ["cuatro"] = 4, ["cinco"] = 5,
                ["seis"] = 6, ["siete"] = 7, ["ocho"] = 8, ["nueve"] = 9,
                ["diez"] = 10, ["once"] = 11, ["doce"] = 12, ["trece"] = 13,
                ["catorce"] = 14, ["quince"] = 15, ["dieciseis"] = 16,
                ["diecisiete"] = 17, ["dieciocho"] = 18, ["diecinueve"] = 19,
                ["veinte"] = 20, ["treinta"] = 30, ["cuarenta"] = 40,
                ["cincuenta"] = 50, ["sesenta"] = 60, ["setenta"] = 70,
                ["ochenta"] = 80, ["noventa"] = 90, ["cien"] = 100,
                ["ciento"] = 100, ["doscientos"] = 200
            };

        private const string NumberToken = @"(\d+(?:[.,]\d+)?|cero|un|uno|una|dos|tres|cuatro|cinco|seis|siete|ocho|nueve|diez|once|doce|trece|catorce|quince|dieciseis|diecisiete|dieciocho|diecinueve|veinte|treinta|cuarenta|cincuenta|sesenta|setenta|ochenta|noventa|cien|ciento|doscientos)";

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

        private static int? ExtractRepetitions(string text)
        {
            var match = RepetitionsWithUnitRegex().Match(text);
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

            match = WeightAfterConRegex().Match(text);
            return match.Success ? ParseDecimal(match.Groups[1].Value) : null;
        }

        private static int? ExtractRir(string text)
        {
            var match = RirRegex().Match(text);
            return match.Success ? ParseInteger(match.Groups[1].Value) : null;
        }

        private static int? ParseInteger(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                return number;

            return NumberWords.TryGetValue(value, out number) ? number : null;
        }

        private static decimal? ParseDecimal(string value)
        {
            var normalized = value.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                return number;

            return ParseInteger(value);
        }

        private static string Normalize(string value)
        {
            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        [GeneratedRegex(NumberToken + @"\s*(?:reps?|repeticiones?)\b", RegexOptions.IgnoreCase)]
        private static partial Regex RepetitionsWithUnitRegex();

        [GeneratedRegex(@"(?:hice|fueron|termine|complete|anotame|anota|registra|registrame)\s+" + NumberToken, RegexOptions.IgnoreCase)]
        private static partial Regex RepetitionsAfterVerbRegex();

        [GeneratedRegex(NumberToken + @"\s*(?:kg|kilos?)\b", RegexOptions.IgnoreCase)]
        private static partial Regex WeightWithUnitRegex();

        [GeneratedRegex(@"\bcon\s+" + NumberToken + @"(?!\s*(?:reps?|repeticiones?))", RegexOptions.IgnoreCase)]
        private static partial Regex WeightAfterConRegex();

        [GeneratedRegex(@"\brir\s*" + NumberToken, RegexOptions.IgnoreCase)]
        private static partial Regex RirRegex();
    }
}
