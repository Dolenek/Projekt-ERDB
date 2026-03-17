using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class AppSettingsService
    {
        private readonly LocalSettingsStore _store;

        public AppSettingsService(LocalSettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Current = ReadSnapshot();
        }

        public AppSettingsSnapshot Current { get; private set; }

        public event Action<AppSettingsSnapshot> SettingsChanged;

        public AppSettingsSnapshot LoadCurrent()
        {
            Current = ReadSnapshot();
            return Current;
        }

        public void Save(AppSettingsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _store.SetString("channel_url", snapshot.ChannelUrl);
            _store.SetBool("use_at_me_fallback", snapshot.UseAtMeFallback);
            _store.SetString("area", snapshot.Area);
            _store.SetString("hunt_ms", snapshot.HuntMs);
            _store.SetString("adventure_ms", snapshot.AdventureMs);
            _store.SetString("work_ms", snapshot.WorkMs);
            _store.SetString("farm_ms", snapshot.FarmMs);
            _store.SetString("lootbox_ms", snapshot.LootboxMs);

            Current = snapshot;
            SettingsChanged?.Invoke(Current);
        }

        private AppSettingsSnapshot ReadSnapshot()
        {
            var defaults = AppSettingsSnapshot.Default;
            return new AppSettingsSnapshot(
                _store.GetString("channel_url", defaults.ChannelUrl),
                _store.GetBool("use_at_me_fallback", defaults.UseAtMeFallback),
                _store.GetString("area", defaults.Area),
                _store.GetString("hunt_ms", defaults.HuntMs),
                _store.GetString("adventure_ms", defaults.AdventureMs),
                _store.GetString("work_ms", defaults.WorkMs),
                _store.GetString("farm_ms", defaults.FarmMs),
                _store.GetString("lootbox_ms", defaults.LootboxMs));
        }
    }
}
