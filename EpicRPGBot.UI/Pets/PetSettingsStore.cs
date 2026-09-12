using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Pets
{
    public sealed class PetSettingsStore
    {
        private readonly LocalSettingsStore _store;
        public PetSettingsStore(LocalSettingsStore store = null)
        {
            _store = store ?? new LocalSettingsStore("pet-settings.ini");
        }
        public PetOptions Load()
        {
            return new PetOptions {
                TimeTravel = int.TryParse(_store.GetString("tt"), out var tt) && tt >= 0 ? (int?)tt : null,
                KeepBest = _store.GetBool("keep-best", true), ProtectSpecial = _store.GetBool("special", true),
                ProtectEpic = _store.GetBool("epic", true), ProtectAscended = _store.GetBool("ascended", true),
                ProtectPerfect = _store.GetBool("perfect", true), MixSpecies = _store.GetBool("mix", false)
            };
        }

        public void Save(PetOptions options)
        {
            _store.SetString("tt", options.TimeTravel?.ToString() ?? "");
            _store.SetBool("keep-best", options.KeepBest);
            _store.SetBool("special", options.ProtectSpecial);
            _store.SetBool("epic", options.ProtectEpic);
            _store.SetBool("ascended", options.ProtectAscended);
            _store.SetBool("perfect", options.ProtectPerfect);
            _store.SetBool("mix", options.MixSpecies);
        }

        public HashSet<string> LoadLocks(PetInventory inventory)
        {
            var encoded = _store.GetString("locks-" + Encode(inventory.Owner), "");
            var ids = new HashSet<string>();
            foreach (var fingerprint in encoded.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var matches = inventory.Pets.Where(p => Encode(p.Fingerprint) == fingerprint).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException(
                    "A saved lock cannot be matched uniquely. Use Reset selections and locks, then select protected pets again.");
                ids.Add(matches[0].Id);
            }
            return ids;
        }

        public void SaveLocks(PetInventory inventory, HashSet<string> lockedIds)
        {
            _store.SetString("locks-" + Encode(inventory.Owner), string.Join(";",
                inventory.Pets.Where(p => lockedIds.Contains(p.Id)).Select(p => Encode(p.Fingerprint))));
        }

        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
