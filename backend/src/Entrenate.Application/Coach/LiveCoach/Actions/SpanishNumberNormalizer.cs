using System.Globalization;
using System.Text;

namespace Entrenate.Application.Coach.LiveCoach.Actions
{
    internal static class SpanishNumberNormalizer
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
                ["veinte"] = 20, ["veintiuno"] = 21, ["veintidos"] = 22,
                ["veintitres"] = 23, ["veinticuatro"] = 24, ["veinticinco"] = 25,
                ["veintiseis"] = 26, ["veintisiete"] = 27, ["veintiocho"] = 28,
                ["veintinueve"] = 29, ["treinta"] = 30, ["cuarenta"] = 40,
                ["cincuenta"] = 50, ["sesenta"] = 60, ["setenta"] = 70,
                ["ochenta"] = 80, ["noventa"] = 90, ["cien"] = 100,
                ["ciento"] = 100, ["doscientos"] = 200, ["trescientos"] = 300,
                ["cuatrocientos"] = 400, ["quinientos"] = 500, ["seiscientos"] = 600,
                ["setecientos"] = 700, ["ochocientos"] = 800, ["novecientos"] = 900,
                ["mil"] = 1000
            };

        public static string NormalizeText(string value)
        {
            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            return NormalizeNumberWords(
                builder.ToString().Normalize(NormalizationForm.FormC));
        }

        public static string NormalizeNumberWords(string text)
        {
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var normalized = new List<string>(tokens.Length);
            for (var index = 0; index < tokens.Length; index++)
            {
                if (!NumberWords.TryGetValue(tokens[index], out var value))
                {
                    normalized.Add(tokens[index]);
                    continue;
                }

                var total = value;
                while (index + 1 < tokens.Length)
                {
                    if (tokens[index + 1] == "y" &&
                        index + 2 < tokens.Length &&
                        NumberWords.TryGetValue(tokens[index + 2], out var afterY) &&
                        afterY < 10)
                    {
                        total += afterY;
                        index += 2;
                        continue;
                    }

                    if (!NumberWords.TryGetValue(tokens[index + 1], out var next) ||
                        next >= 1000 ||
                        next >= total)
                    {
                        break;
                    }

                    total += next;
                    index++;
                }

                normalized.Add(total.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join(' ', normalized);
        }

        public static bool TryParseInteger(string value, out int number)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                return true;

            return NumberWords.TryGetValue(value, out number);
        }
    }
}
