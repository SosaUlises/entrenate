using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;

namespace Entrenate.Application.Common.Interfaces
{
    public interface ILiveCoachSpeechToText
    {
        Task<LiveCoachTranscriptResult> TranscribeAsync(
            LiveCoachAudioInput input,
            CancellationToken cancellationToken = default);
    }
}

