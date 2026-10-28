using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Queries.GenerateSessionAdaptationProposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using FluentValidation;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Preview
{
    public sealed class GenerateSessionAdaptationProposalQueryHandlerTests
    {
        private const string UserId = "test-user-1";

        [Fact]
        public async Task Handle_ValidRequest_GeneratesPreviewWithoutPersisting()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var handler = fixture.CreatePreviewHandler();

            var query = new GenerateSessionAdaptationProposalQuery(
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: null, // should default to profile equipment
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.ProposalId));
            Assert.False(string.IsNullOrWhiteSpace(result.ProposalHash));
            Assert.Equal(SessionAdaptationProposalEngine.PolicyVersion, result.PolicyVersion);
            Assert.Equal("FeasibleWithChanges", result.Feasibility);
            Assert.Equal(day.Id, result.Context.RoutineDayId);
            Assert.Equal(45, result.Context.AvailableMinutes);
            // Profile equipment is barbell and dumbbell (2 items)
            Assert.Equal(2, result.Context.AvailableEquipmentIds.Count);
            Assert.NotEmpty(result.OriginalExercises);
            Assert.NotEmpty(result.AdaptedExercises);
            // No sessions should have been created in the session repository
            Assert.Empty(fixture.SessionRepository.AddedSessions);
        }

        [Fact]
        public async Task Handle_NotFeasible_ReturnsValidProposalWithNotFeasible()
        {
            var fixture = new AdaptiveTestFixture();
            // Setup a day with high minimum time
            var day = fixture.SetupRoutineDayWithMultipleCompounds();
            var handler = fixture.CreatePreviewHandler();

            var query = new GenerateSessionAdaptationProposalQuery(
                RoutineDayId: day.Id,
                AvailableMinutes: 15, // strictly below minimum required duration
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("NotFeasible", result.Feasibility);
            Assert.Contains(result.Diagnostics, d => d.Code == "MinimumSessionExceedsAvailableTime");
            Assert.Empty(fixture.SessionRepository.AddedSessions);
        }

        [Fact]
        public async Task Handle_NonExistentRoutineDay_ThrowsNotFoundException()
        {
            var fixture = new AdaptiveTestFixture();
            fixture.SetupRoutineDay();
            var handler = fixture.CreatePreviewHandler();

            var query = new GenerateSessionAdaptationProposalQuery(
                RoutineDayId: Guid.NewGuid(), // non-existent
                AvailableMinutes: 45,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(query, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_InactiveRoutine_ThrowsConflictException()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            day.Rutina.Desactivar(); // Routine is inactive
            var handler = fixture.CreatePreviewHandler();

            var query = new GenerateSessionAdaptationProposalQuery(
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(query, CancellationToken.None));

            Assert.Contains("activa", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_EmptyAvailableEquipmentIds_DoesNotFallbackToProfile()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var handler = fixture.CreatePreviewHandler();

            // When AvailableEquipmentIds is empty array ([]), user has no equipment available today
            var query = new GenerateSessionAdaptationProposalQuery(
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: Array.Empty<Guid>(),
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            // Must NOT fallback to profile equipment (which has 2 items); must stay empty
            Assert.Empty(result.Context.AvailableEquipmentIds);
        }

        [Fact]
        public async Task Handle_ExcludedExerciseNotBelongingToDay_ThrowsValidationException()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var handler = fixture.CreatePreviewHandler();

            var query = new GenerateSessionAdaptationProposalQuery(
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: new[] { Guid.NewGuid() }, // foreign exercise ID
                EffortPreference: SessionEffortPreference.Normal);

            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(query, CancellationToken.None));
        }
    }
}

