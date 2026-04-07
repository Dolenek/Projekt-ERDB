using System;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class TrackedCommandScheduler
    {
        private static bool LooksLikeTrackedCommandResponse(Models.DiscordMessageSnapshot snapshot)
            => TrackedCommandResponseClassifier.LooksLikeTrackedCommandResponse(snapshot);

        private static bool TryInferKind(string message, out TrackedCommandKind kind)
            => TrackedCommandResponseClassifier.TryInferKind(message, out kind);

        private static bool TryParseWaitAtLeast(string message, out TimeSpan delay)
            => TrackedCommandResponseClassifier.TryParseWaitAtLeast(message, out delay);
    }
}
