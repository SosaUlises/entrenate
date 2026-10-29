using System;
using Microsoft.AspNetCore.Http;

namespace Entrenate.Api.Requests.Coach
{
    public sealed class TranscribeLiveAudioRequest
    {
        public IFormFile? Audio { get; set; }

        public Guid? InteractionId { get; set; }

        public string? ClientTurnId { get; set; }

        public string? MimeType { get; set; }

        public int? DurationMilliseconds { get; set; }
    }
}

