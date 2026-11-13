using System;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Entrenate.Application.Coach.LiveCoach.Commands.SynthesizeLiveSpeech
{
    public sealed class SynthesizeLiveSpeechCommandHandler : IRequestHandler<SynthesizeLiveSpeechCommand, LiveCoachSpeechAudio>
    {
        private readonly ILiveCoachResponseCache _responseCache;
        private readonly ILiveCoachTextToSpeech _textToSpeech;
        private readonly ILogger<SynthesizeLiveSpeechCommandHandler> _logger;

        public SynthesizeLiveSpeechCommandHandler(
            ILiveCoachResponseCache responseCache,
            ILiveCoachTextToSpeech textToSpeech,
            ILogger<SynthesizeLiveSpeechCommandHandler> logger)
        {
            _responseCache = responseCache;
            _textToSpeech = textToSpeech;
            _logger = logger;
        }

        public async Task<LiveCoachSpeechAudio> Handle(
            SynthesizeLiveSpeechCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ResponseId == Guid.Empty)
            {
                throw new ArgumentException("El ID de respuesta es obligatorio.", nameof(request.ResponseId));
            }

            // 1. Authoritative cache lookup - only previously approved Coach text is synthesized
            if (!_responseCache.TryGet(request.ResponseId, out var approvedText) || string.IsNullOrWhiteSpace(approvedText))
            {
                _logger.LogWarning(
                    "TTS requested for unknown or expired responseId: {ResponseId}",
                    request.ResponseId);

                throw new NotFoundException("La respuesta solicitada no existe o ha expirado.");
            }

            // 2. Synthesize official response text via provider abstraction
            try
            {
                return await _textToSpeech.SynthesizeAsync(approvedText, cancellationToken);
            }
            catch (ExternalServiceException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Live Coach TTS synthesis failed for responseId: {ResponseId}",
                    request.ResponseId);

                throw new ExternalServiceException("El servicio de síntesis de voz no está disponible en este momento.", ex);
            }
        }
    }
}
