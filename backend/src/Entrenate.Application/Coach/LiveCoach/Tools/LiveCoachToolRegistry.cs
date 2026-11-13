using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Common.Interfaces;

namespace Entrenate.Application.Coach.LiveCoach.Tools
{
    public interface ILiveCoachToolRegistry
    {
        LiveCoachToolResult<LiveCoachExerciseContext> GetCurrentExercise(LiveCoachSessionContext context);

        LiveCoachToolResult<LiveCoachSetContext> GetCurrentSet(LiveCoachSessionContext context);

        LiveCoachToolResult<IReadOnlyList<LiveCoachCompletedSetContext>> GetCompletedSets(
            LiveCoachSessionContext context,
            bool sessionScope = false);

        Task<LiveCoachToolResult<PreviousPerformanceData>> GetPreviousPerformanceAsync(
            LiveCoachSessionContext context,
            string userId,
            CancellationToken cancellationToken = default);

        LiveCoachToolResult<LiveCoachPrescriptionContext> GetCurrentPrescription(LiveCoachSessionContext context);

        LiveCoachToolResult<LiveCoachRestContext> GetRestTimer(LiveCoachSessionContext context);

        LiveCoachToolResult<LiveCoachSessionSummaryContext> GetSessionSummary(LiveCoachSessionContext context);
    }

    public sealed class LiveCoachToolRegistry : ILiveCoachToolRegistry
    {
        private readonly ITrainingSessionRepository _sessionRepository;

        public LiveCoachToolRegistry(ITrainingSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public LiveCoachToolResult<LiveCoachExerciseContext> GetCurrentExercise(LiveCoachSessionContext context)
        {
            if (context.CurrentExercise == null)
            {
                return new LiveCoachToolResult<LiveCoachExerciseContext>(
                    LiveCoachToolStatus.NotAvailable,
                    null,
                    "No hay un ejercicio activo en la sesión.");
            }

            return new LiveCoachToolResult<LiveCoachExerciseContext>(
                LiveCoachToolStatus.Available,
                context.CurrentExercise);
        }

        public LiveCoachToolResult<LiveCoachSetContext> GetCurrentSet(LiveCoachSessionContext context)
        {
            if (context.CurrentSet == null)
            {
                return new LiveCoachToolResult<LiveCoachSetContext>(
                    LiveCoachToolStatus.NotAvailable,
                    null,
                    "No hay una serie activa para el ejercicio actual.");
            }

            return new LiveCoachToolResult<LiveCoachSetContext>(
                LiveCoachToolStatus.Available,
                context.CurrentSet);
        }

        public LiveCoachToolResult<IReadOnlyList<LiveCoachCompletedSetContext>> GetCompletedSets(
            LiveCoachSessionContext context,
            bool sessionScope = false)
        {
            var sets = context.CompletedSets;
            if (!sessionScope && context.CurrentExercise != null)
            {
                sets = sets.Where(s => s.ExerciseName == context.CurrentExercise.Name).ToList();
            }

            return new LiveCoachToolResult<IReadOnlyList<LiveCoachCompletedSetContext>>(
                LiveCoachToolStatus.Available,
                sets);
        }

        public async Task<LiveCoachToolResult<PreviousPerformanceData>> GetPreviousPerformanceAsync(
            LiveCoachSessionContext context,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (context.CurrentExercise == null)
            {
                return new LiveCoachToolResult<PreviousPerformanceData>(
                    LiveCoachToolStatus.NotAvailable,
                    null,
                    "No hay un ejercicio activo para consultar historial.");
            }

            try
            {
                var exposures = await _sessionRepository.GetRecentCompletedExerciseExposuresByUserIdAsync(
                    context.CurrentExercise.ExerciseId,
                    userId,
                    cancellationToken);

                if (exposures == null || exposures.Count == 0)
                {
                    return new LiveCoachToolResult<PreviousPerformanceData>(
                        LiveCoachToolStatus.NotAvailable,
                        null,
                        $"No hay registros previos para {context.CurrentExercise.Name}.");
                }

                var items = exposures
                    .OrderByDescending(e => e.StartedAt)
                    .Take(3)
                    .SelectMany(e => e.CompletedSets.Select(s => new PreviousPerformanceItem(
                        e.StartedAt,
                        s.Weight,
                        s.Repetitions,
                        s.Rir)))
                    .ToList();

                return new LiveCoachToolResult<PreviousPerformanceData>(
                    LiveCoachToolStatus.Available,
                    new PreviousPerformanceData(context.CurrentExercise.Name, items));
            }
            catch
            {
                return new LiveCoachToolResult<PreviousPerformanceData>(
                    LiveCoachToolStatus.Failed,
                    null,
                    "No se pudo consultar el historial del ejercicio.");
            }
        }

        public LiveCoachToolResult<LiveCoachPrescriptionContext> GetCurrentPrescription(LiveCoachSessionContext context)
        {
            if (context.CurrentPrescription == null)
            {
                return new LiveCoachToolResult<LiveCoachPrescriptionContext>(
                    LiveCoachToolStatus.NotAvailable,
                    null,
                    "No hay prescripción disponible para el ejercicio actual.");
            }

            return new LiveCoachToolResult<LiveCoachPrescriptionContext>(
                LiveCoachToolStatus.Available,
                context.CurrentPrescription);
        }

        public LiveCoachToolResult<LiveCoachRestContext> GetRestTimer(LiveCoachSessionContext context)
        {
            return new LiveCoachToolResult<LiveCoachRestContext>(
                LiveCoachToolStatus.Available,
                context.Rest);
        }

        public LiveCoachToolResult<LiveCoachSessionSummaryContext> GetSessionSummary(LiveCoachSessionContext context)
        {
            return new LiveCoachToolResult<LiveCoachSessionSummaryContext>(
                LiveCoachToolStatus.Available,
                context.Summary);
        }
    }
}

