using System.Security.Cryptography;
using System.Text;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public static class LiveTimeConstraintFingerprint
    {
        public static string Create(SesionEntrenamiento session)
        {
            ArgumentNullException.ThrowIfNull(session);
            var builder = new StringBuilder();
            builder.Append(session.Id).Append('|').Append(session.Estado);

            foreach (var exercise in session.Ejercicios.OrderBy(item => item.Orden))
            {
                builder.Append('|')
                    .Append(exercise.Id).Append(':')
                    .Append(exercise.Orden).Append(':')
                    .Append(exercise.SeriesObjetivo).Append(':')
                    .Append(exercise.Omitido ? '1' : '0').Append(':');

                foreach (var number in exercise.Series
                    .Where(set => set.EstaRealmenteCompletada())
                    .Select(set => set.NumeroSerie)
                    .Where(number => number >= 1 && number <= exercise.SeriesObjetivo)
                    .Distinct()
                    .OrderBy(number => number))
                {
                    builder.Append(number).Append(',');
                }
            }

            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
        }
    }
}
