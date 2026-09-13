using System;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed class DiscordWebViewLease
    {
        private DiscordWebViewSession _session;
        private readonly DiscordWebViewActivityReason _reason;

        internal DiscordWebViewLease(
            DiscordWebViewSession session,
            DiscordWebViewActivityReason reason)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _reason = reason;
        }

        public Task ReleaseAsync()
        {
            var session = _session;
            _session = null;
            return session == null
                ? Task.CompletedTask
                : session.ReleaseLeaseAsync(_reason);
        }
    }
}
