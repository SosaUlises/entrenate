using System;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Api.Requests.Coach;
using Entrenate.Application.Coach.LiveCoach.Commands.TranscribeLiveAudio;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/coach/live")]
    public sealed class CoachLiveController : ControllerBase
    {
        private readonly ISender _sender;

        public CoachLiveController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("transcriptions")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Transcribe(
            [FromForm] TranscribeLiveAudioRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Audio == null || request.Audio.Length == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Audio vacío",
                    Detail = "El archivo de audio es obligatorio.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["code"] = "EmptyAudio" }
                });
            }

            if (string.IsNullOrWhiteSpace(request.ClientTurnId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "TurnId obligatorio",
                    Detail = "El campo clientTurnId es obligatorio.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["code"] = "MissingClientTurnId" }
                });
            }

            var interactionId = request.InteractionId.HasValue && request.InteractionId.Value != Guid.Empty
                ? request.InteractionId.Value
                : Guid.NewGuid();

            var mediaType = !string.IsNullOrWhiteSpace(request.MimeType)
                ? request.MimeType
                : (!string.IsNullOrWhiteSpace(request.Audio.ContentType) ? request.Audio.ContentType : "audio/webm");

            await using var stream = request.Audio.OpenReadStream();

            var command = new TranscribeLiveAudioCommand(
                stream,
                mediaType,
                request.Audio.Length,
                interactionId,
                request.ClientTurnId,
                request.DurationMilliseconds);

            var result = await _sender.Send(command, cancellationToken);

            return result.Status switch
            {
                LiveCoachTranscriptStatus.Success => Ok(new LiveCoachTranscriptionResponseDto(
                    interactionId,
                    request.ClientTurnId,
                    result.Transcript,
                    result.Confidence,
                    result.DetectedLocale,
                    result.IsFinal,
                    "Success")),

                LiveCoachTranscriptStatus.EmptyAudio => BadRequest(new ProblemDetails
                {
                    Title = "Audio vacío",
                    Detail = "El archivo de audio no contiene datos suficientes.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["code"] = "EmptyAudio" }
                }),

                LiveCoachTranscriptStatus.UnsupportedAudio => BadRequest(new ProblemDetails
                {
                    Title = "Audio no soportado",
                    Detail = "El formato de audio enviado no es compatible.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["code"] = "UnsupportedAudio" }
                }),

                LiveCoachTranscriptStatus.AudioTooLarge => StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    new ProblemDetails
                    {
                        Title = "Audio demasiado grande",
                        Detail = "El archivo de audio supera el límite de 2 MB.",
                        Status = StatusCodes.Status413PayloadTooLarge,
                        Extensions = { ["code"] = "AudioPayloadTooLarge" }
                    }),

                LiveCoachTranscriptStatus.AudioTooLong => BadRequest(new ProblemDetails
                {
                    Title = "Duración excedida",
                    Detail = "El audio supera la duración máxima permitida de 15 segundos.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["code"] = "AudioDurationExceeded" }
                }),

                LiveCoachTranscriptStatus.EmptyTranscript => UnprocessableEntity(new ProblemDetails
                {
                    Title = "Transcripción vacía",
                    Detail = "No se detectó habla comprensible en la grabación.",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions = { ["code"] = "EmptyTranscript" }
                }),

                LiveCoachTranscriptStatus.LowConfidence => UnprocessableEntity(new ProblemDetails
                {
                    Title = "Baja confianza",
                    Detail = "No se pudo transcribir con suficiente claridad.",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions = { ["code"] = "LowConfidence" }
                }),

                LiveCoachTranscriptStatus.Timeout => StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new ProblemDetails
                    {
                        Title = "Tiempo de espera agotado",
                        Detail = "El servicio de transcripción agotó el tiempo de espera.",
                        Status = StatusCodes.Status504GatewayTimeout,
                        Extensions = { ["code"] = "Timeout" }
                    }),

                LiveCoachTranscriptStatus.ProviderUnavailable => StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new ProblemDetails
                    {
                        Title = "Servicio no disponible",
                        Detail = "El servicio de transcripción no está disponible en este momento.",
                        Status = StatusCodes.Status503ServiceUnavailable,
                        Extensions = { ["code"] = "ProviderUnavailable" }
                    }),

                LiveCoachTranscriptStatus.Canceled => StatusCode(
                    499,
                    new ProblemDetails
                    {
                        Title = "Operación cancelada",
                        Detail = "La solicitud fue cancelada por el cliente.",
                        Status = 499,
                        Extensions = { ["code"] = "Canceled" }
                    }),

                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Title = "Error de transcripción",
                        Detail = "Ocurrió un error inesperado al procesar el audio.",
                        Status = StatusCodes.Status500InternalServerError
                    })
            };
        }
    }
}

