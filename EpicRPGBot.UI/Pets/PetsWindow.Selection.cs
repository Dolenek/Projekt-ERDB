using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EpicRPGBot.UI.Pets
{
    public partial class PetsWindow
    {
        private PetOptions ReadOptions() => new PetOptions {
            TimeTravel = int.TryParse(TtBox.Text, out var tt) && tt >= 0 ? (int?)tt : null,
            KeepBest = KeepBestBox.IsChecked == true, ProtectSpecial = SpecialBox.IsChecked == true,
            ProtectEpic = EpicBox.IsChecked == true, ProtectAscended = AscendedBox.IsChecked == true,
            ProtectPerfect = PerfectBox.IsChecked == true, MixSpecies = MixBox.IsChecked == true
        };

        private void LoadOptions(PetOptions options)
        {
            TtBox.Text = options.TimeTravel?.ToString() ?? "";
            KeepBestBox.IsChecked = options.KeepBest;
            SpecialBox.IsChecked = options.ProtectSpecial;
            EpicBox.IsChecked = options.ProtectEpic;
            AscendedBox.IsChecked = options.ProtectAscended;
            PerfectBox.IsChecked = options.ProtectPerfect;
            MixBox.IsChecked = options.MixSpecies;
        }

        private PetFusionRequest ReadRequest() => new PetFusionRequest {
            Options = ReadOptions(), Mode = (PetFusionMode)Math.Max(0, ModeBox.SelectedIndex),
            TargetId = TargetBox.SelectedItem as string ?? "",
            Goal = int.TryParse(GoalBox.Text, out var goal) ? goal : 0,
            LockedIds = new HashSet<string>(_rows.Where(r => r.Locked).Select(r => r.Id)),
            MaterialIds = new HashSet<string>(_rows.Where(r => r.Selected).Select(r => r.Id))
        };

        private void RefreshPreview()
        {
            if (!_ready || _inventory == null) return;
            var request = ReadRequest();
            TargetBox.IsEnabled = request.Mode == PetFusionMode.Upgrade;
            GoalBox.IsEnabled = request.Mode != PetFusionMode.Manual;
            foreach (var row in _rows) row.Protection = PetProtection.Reason(row.Pet, _inventory, request);
            var step = _planner.Next(_inventory, request);
            var parents = string.Join(", ", step.Parents.Select(p => p.Id + " " + p.Species + " T" + p.Tier));
            var species = string.Join(" / ", PetProtection.ResultSpecies(step.Parents));
            PreviewText.Text = _blocked ? "Inventory needs review. Refresh or explicitly reset selections and locks." :
                "Goal: " + request.Mode + " " + (request.Mode == PetFusionMode.Manual ? "" : request.Goal.ToString()) +
                " · Selected material: " + request.MaterialIds.Count + " · " + step.Reason +
                (step.Available ? "\nConsumes: " + parents + ". Result species: " + species +
                (request.Mode == PetFusionMode.Manual ? ". ALL selected pets fuse together in ONE command." :
                ". One pair per command; reload IDs before the next pair.") +
                " Skills may be lost. Tier-up is not guaranteed." : "");
            StartButton.IsEnabled = !_blocked && _cancellation == null && step.Available;
        }

        private void RowChanged(object sender, PropertyChangedEventArgs args)
        {
            if (!_ready || args.PropertyName == nameof(PetRow.Protection)) return;
            RefreshPreview();
            if (!_blocked) SaveSelections();
        }

        private void SaveSelections()
        {
            _settings.Save(ReadOptions());
            if (_inventory != null) _settings.SaveLocks(_inventory, ReadRequest().LockedIds);
        }

        private void Options_Changed(object sender, RoutedEventArgs args) { RefreshPreview(); }
        private void Plan_Changed(object sender, SelectionChangedEventArgs args) { RefreshPreview(); }
        private void Filter_Changed(object sender, TextChangedEventArgs args)
        {
            if (!_ready) return;
            CollectionViewSource.GetDefaultView(_rows).Refresh();
            RefreshPreview();
        }

        private bool FilterPet(object entry)
        {
            if (!(entry is PetRow row)) return false;
            var searchable = row.Id + " " + row.Species + " " + row.Skills + " " + row.Status;
            return Contains(searchable, SearchBox.Text) && Contains(row.Species, SpeciesBox.Text) &&
                Contains(row.Skills, SkillBox.Text) && (string.IsNullOrWhiteSpace(TierFilterBox.Text) ||
                row.Tier.ToString() == TierFilterBox.Text.Trim() ||
                row.Tier == PetPageParser.RomanTier(TierFilterBox.Text.Trim()));
        }

        private static bool Contains(string value, string search) => value.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;

        private void Select_Click(object sender, RoutedEventArgs args)
        {
            if (_inventory == null || _blocked) return;
            var request = ReadRequest();
            _ready = false;
            foreach (var row in _rows) row.Selected = PetProtection.Reason(row.Pet, _inventory, request) == "";
            _ready = true;
            RefreshPreview();
        }
    }
}
