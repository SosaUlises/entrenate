using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;

namespace Entrenate.Application.Common.Interfaces
{
    public interface ILiveCoachTextToSpeech
    {
        Task<LiveCoachSpeechAudio> SynthesizeAsync(
            string text,
            CancellationToken cancellationToken = default);
    }
}

