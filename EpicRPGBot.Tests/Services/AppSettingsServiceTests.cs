using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class AppSettingsServiceTests
{
    [Fact]
    public void HealAfterHuntAndAdventure_DefaultsToFalseAndPersists()
    {
        var fileName = "heal-after-hunt-adventure-" + Guid.NewGuid() + ".ini";
        var filePath = GetSettingsFilePath(fileName);

        try
        {
            Assert.False(AppSettingsSnapshot.Default.HealAfterHuntAndAdventure);
            var service = new AppSettingsService(new LocalSettingsStore(fileName));
            Assert.False(service.Current.HealAfterHuntAndAdventure);

            service.Save(service.Current.WithHealAfterHuntAndAdventure(true));
            var reloaded = new AppSettingsService(new LocalSettingsStore(fileName));

            Assert.True(reloaded.Current.HealAfterHuntAndAdventure);
            Assert.True(reloaded.Current.WithArea("12").HealAfterHuntAndAdventure);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void HardcoreHuntAndAdventure_DefaultsToFalseAndPersists()
    {
        var fileName = "hardcore-commands-" + Guid.NewGuid() + ".ini";
        var filePath = GetSettingsFilePath(fileName);

        try
        {
            Assert.False(AppSettingsSnapshot.Default.UseHardcoreHuntAndAdventure);
            var service = new AppSettingsService(new LocalSettingsStore(fileName));
            Assert.False(service.Current.UseHardcoreHuntAndAdventure);

            service.Save(service.Current.WithHardcoreHuntAndAdventure(true));
            var reloaded = new AppSettingsService(new LocalSettingsStore(fileName));

            Assert.True(reloaded.Current.UseHardcoreHuntAndAdventure);
            Assert.True(reloaded.Current.WithArea("12").UseHardcoreHuntAndAdventure);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void GuardForegroundSetting_DefaultsToFalseAndPersists()
    {
        var fileName = "guard-foreground-" + Guid.NewGuid() + ".ini";
        var filePath = GetSettingsFilePath(fileName);

        try
        {
            Assert.False(AppSettingsSnapshot.Default.BringGuardAlertsToForeground);
            var service = new AppSettingsService(new LocalSettingsStore(fileName));
            Assert.False(service.Current.BringGuardAlertsToForeground);

            service.Save(service.Current.WithBringGuardAlertsToForeground(true));
            var reloaded = new AppSettingsService(new LocalSettingsStore(fileName));

            Assert.True(reloaded.Current.BringGuardAlertsToForeground);
            Assert.True(reloaded.Current.WithArea("12").BringGuardAlertsToForeground);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void GuildWatcherActive_DefaultsToFalseAndPersistsAcrossOtherChanges()
    {
        var fileName = "guild-watcher-active-" + Guid.NewGuid() + ".ini";
        var filePath = GetSettingsFilePath(fileName);

        try
        {
            Assert.False(AppSettingsSnapshot.Default.GuildRaidWatcherActive);
            var service = new AppSettingsService(new LocalSettingsStore(fileName));
            Assert.False(service.Current.GuildRaidWatcherActive);

            service.Save(service.Current.WithGuildRaidWatcherActive(true));
            var reloaded = new AppSettingsService(new LocalSettingsStore(fileName));

            Assert.True(reloaded.Current.GuildRaidWatcherActive);
            Assert.True(reloaded.Current.WithArea("12").GuildRaidWatcherActive);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string GetSettingsFilePath(string fileName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EpicRPGBot.UI",
            "settings",
            fileName);
    }
}
