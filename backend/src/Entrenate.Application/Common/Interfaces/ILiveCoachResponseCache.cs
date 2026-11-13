using System;

namespace Entrenate.Application.Common.Interfaces
{
    public interface ILiveCoachResponseCache
    {
        void Set(Guid responseId, string text, TimeSpan ttl);

        bool TryGet(Guid responseId, out string? text);
    }
}

