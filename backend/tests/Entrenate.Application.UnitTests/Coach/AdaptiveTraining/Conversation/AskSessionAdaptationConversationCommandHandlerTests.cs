using System.Text.Json;
using Entrenate.Application.Coach.AdaptiveTraining.Conversation.Commands.AskSessionAdaptationConversation;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Common.Models;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Conversation
{
    public sealed class AskSessionAdaptationConversationCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidRequest_CallsProviderAndReturnsAnswer()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var provider = new FakeCoachConversationProvider();
            var handler = new AskSessionAdaptationConversationCommandHandler(
                fixture.CurrentUserService,
                fixture.Generator,
                provider);

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: [AdaptiveTestFixture.DumbbellId],
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Lighter);

            const string question = "¿Por qué reemplazaste el press de banca?";

            var command = new AskSessionAdaptationConversationCommand(
                proposal.AdaptationProposalId.ToString(),
                proposal.ProposalHash,
                day.Id,
                45,
                [AdaptiveTestFixture.DumbbellId],
                null,
                SessionEffortPreference.Lighter,
                question);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal("Respuesta explicativa del Coach.", result.Answer);
            Assert.Equal(1, provider.CallCount);
            Assert.NotNull(provider.LastPrompt);

            // Verify prompt input JSON carries the question and current proposal context
            using var doc = JsonDocument.Parse(provider.LastPrompt.Input);
            var root = doc.RootElement;
            Assert.Equal(question, root.GetProperty("question").GetString());

            var context = root.GetProperty("context");
            Assert.Equal("Factible con adaptaciones", context.GetProperty("feasibility").GetString());
            Assert.Equal(45, context.GetProperty("availableMinutes").GetInt32());
            Assert.Equal("Más liviana", context.GetProperty("effortPreference").GetString());
        }

        [Fact]
        public async Task Handle_StaleProposalId_ThrowsConflictExceptionAndDoesNotCallProvider()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var provider = new FakeCoachConversationProvider();
            var handler = new AskSessionAdaptationConversationCommandHandler(
                fixture.CurrentUserService,
                fixture.Generator,
                provider);

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new AskSessionAdaptationConversationCommand(
                Guid.NewGuid().ToString(), // Different/stale proposal ID
                proposal.ProposalHash,
                day.Id,
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                "¿Por qué cambiaste la sesión?");

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(command, CancellationToken.None));

            Assert.Contains("regenerarse", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, provider.CallCount);
        }

        [Fact]
        public async Task Handle_StaleProposalHash_ThrowsConflictExceptionAndDoesNotCallProvider()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var provider = new FakeCoachConversationProvider();
            var handler = new AskSessionAdaptationConversationCommandHandler(
                fixture.CurrentUserService,
                fixture.Generator,
                provider);

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new AskSessionAdaptationConversationCommand(
                proposal.AdaptationProposalId.ToString(),
                "stale-hash-value-00000000000000000000000000000000",
                day.Id,
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                "¿Por qué cambiaste la sesión?");

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(command, CancellationToken.None));

            Assert.Contains("regenerarse", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, provider.CallCount);
        }

        [Fact]
        public async Task Handle_NotFeasibleProposal_AllowsConversationAndCallsProvider()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDayWithMultipleCompounds();
            var provider = new FakeCoachConversationProvider();
            var handler = new AskSessionAdaptationConversationCommandHandler(
                fixture.CurrentUserService,
                fixture.Generator,
                provider);

            // Available time is 15 minutes, which is impossible for multiple compounds -> NotFeasible
            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 15,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            Assert.Equal(SessionAdaptationFeasibility.NotFeasible, proposal.Feasibility);

            var command = new AskSessionAdaptationConversationCommand(
                proposal.AdaptationProposalId.ToString(),
                proposal.ProposalHash,
                day.Id,
                15,
                null,
                null,
                SessionEffortPreference.Normal,
                "¿Por qué no entra en el tiempo?");

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal("Respuesta explicativa del Coach.", result.Answer);
            Assert.Equal(1, provider.CallCount);

            using var doc = JsonDocument.Parse(provider.LastPrompt!.Input);
            var root = doc.RootElement;
            var context = root.GetProperty("context");
            Assert.Equal("No factible", context.GetProperty("feasibility").GetString());
            Assert.False(context.GetProperty("canBeConfirmed").GetBoolean());
        }

        [Fact]
        public async Task Handle_ProviderFailure_ThrowsExternalServiceException()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var provider = new FakeCoachConversationProvider { ThrowOnCall = true };
            var handler = new AskSessionAdaptationConversationCommandHandler(
                fixture.CurrentUserService,
                fixture.Generator,
                provider);

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new AskSessionAdaptationConversationCommand(
                proposal.AdaptationProposalId.ToString(),
                proposal.ProposalHash,
                day.Id,
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                "¿Cuánto dura?");

            var ex = await Assert.ThrowsAsync<ExternalServiceException>(() =>
                handler.Handle(command, CancellationToken.None));

            Assert.Contains("No se pudo obtener una respuesta de Entrenate Coach", ex.Message);
        }

        [Fact]
        public async Task Handle_CancelledToken_PropagatesCancellation()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var provider = new FakeCoachConversationProvider();
            var handler = new AskSessionAdaptationConversationCommandHandler(
                fixture.CurrentUserService,
                fixture.Generator,
                provider);

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var command = new AskSessionAdaptationConversationCommand(
                proposal.AdaptationProposalId.ToString(),
                proposal.ProposalHash,
                day.Id,
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                "¿Por qué?");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                handler.Handle(command, cts.Token));
        }

        [Fact]
        public void Validator_RejectsEmptyWhitespaceAndOverLimitQuestions()
        {
            var validator = new AskSessionAdaptationConversationCommandValidator();

            var empty = validator.Validate(new AskSessionAdaptationConversationCommand(
                Guid.NewGuid().ToString(),
                "hash",
                Guid.NewGuid(),
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                ""));

            var whitespace = validator.Validate(new AskSessionAdaptationConversationCommand(
                Guid.NewGuid().ToString(),
                "hash",
                Guid.NewGuid(),
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                "   \t\n   "));

            var over500 = validator.Validate(new AskSessionAdaptationConversationCommand(
                Guid.NewGuid().ToString(),
                "hash",
                Guid.NewGuid(),
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                new string('a', 501)));

            var valid = validator.Validate(new AskSessionAdaptationConversationCommand(
                Guid.NewGuid().ToString(),
                "hash",
                Guid.NewGuid(),
                45,
                null,
                null,
                SessionEffortPreference.Normal,
                "¿Por qué se redujeron las series?"));

            Assert.False(empty.IsValid);
            Assert.Contains(empty.Errors, e => e.PropertyName == "Question");

            Assert.False(whitespace.IsValid);
            Assert.Contains(whitespace.Errors, e => e.PropertyName == "Question");

            Assert.False(over500.IsValid);
            Assert.Contains(over500.Errors, e => e.PropertyName == "Question");

            Assert.True(valid.IsValid);
        }

        private sealed class FakeCoachConversationProvider : ICoachConversationProvider
        {
            public int CallCount { get; private set; }
            public CoachConversationPrompt? LastPrompt { get; private set; }
            public bool ThrowOnCall { get; set; }

            public Task<CoachConversationResponse> GenerateResponseAsync(
                CoachConversationPrompt prompt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ThrowOnCall)
                {
                    throw new InvalidOperationException("Groq API timeout simulation.");
                }

                CallCount++;
                LastPrompt = prompt;

                return Task.FromResult(new CoachConversationResponse("Respuesta explicativa del Coach."));
            }
        }
    }
}

