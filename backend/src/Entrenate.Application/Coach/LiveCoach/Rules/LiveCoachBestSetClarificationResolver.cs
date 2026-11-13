using Entrenate.Application.Coach.LiveCoach.Clarifications;

namespace Entrenate.Application.Coach.LiveCoach.Rules
{
    public static class LiveCoachBestSetClarificationResolver
    {
        public static LiveCoachBestSetCriterion? ParseCriterion(string normalizedText)
        {
            return normalizedText switch
            {
                "peso" or "por peso" or "mas peso" => LiveCoachBestSetCriterion.Weight,
                "repeticiones" or "reps" or "por repeticiones" or "por reps" or
                    "mas repeticiones" or "mas reps" => LiveCoachBestSetCriterion.Repetitions,
                "volumen" or "por volumen" or "mas volumen" => LiveCoachBestSetCriterion.Volume,
                _ => null
            };
        }
    }
}
