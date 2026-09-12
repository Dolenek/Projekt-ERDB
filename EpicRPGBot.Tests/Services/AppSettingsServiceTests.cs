using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class AppSettingsServiceTests
{
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

    private static string GetSettingsFilePath(string fileName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EpicRPGBot.UI",
            "settings",
            fileName);
    }
}
