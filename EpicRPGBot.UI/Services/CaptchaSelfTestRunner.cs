using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EpicRPGBot.UI.Captcha;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaSelfTestRunner
    {
        public async Task RunAsync(Action<string> log)
        {
            try
            {
                var settings = CaptchaSettings.LoadDefault();
                if (!Directory.Exists(settings.SelfTestReplayDirectory))
                {
                    log?.Invoke("[selftest] Set CAPTCHA_SELFTEST_REPLAY_DIR to a directory of labeled attachments.");
                    return;
                }
                var provider = await Task.Run(() => new CaptchaProviderFactory().Create(settings));
                try { await ReplayAsync(settings, provider, log); }
                finally { (provider as IDisposable)?.Dispose(); }
            }
            catch (Exception ex) { log?.Invoke("[selftest] Error: " + ex.Message); }
        }

        private static async Task ReplayAsync(CaptchaSettings settings, ICaptchaAnswerProvider provider, Action<string> log)
        {
            var catalog = CaptchaItemCatalog.Load(settings.ItemNamesFile);
            var correct = 0; var wrong = 0; var rejected = 0; var errors = 0;
            foreach (var file in EnumerateImages(settings.SelfTestReplayDirectory))
            {
                if (!catalog.TryResolveExpectedLabelFromFileName(file, out var expected)) continue;
                try
                {
                    var result = await provider.SolveAsync(File.ReadAllBytes(file), default);
                    if (!result.IsMatch) rejected++;
                    else if (string.Equals(result.Label, expected, StringComparison.OrdinalIgnoreCase)) correct++;
                    else wrong++;
                    log?.Invoke("[selftest] " + Path.GetFileName(file) + " => " +
                        (result.IsMatch ? result.Label : "<rejected>") + "; " + result.Detail);
                }
                catch (Exception ex) { errors++; log?.Invoke("[selftest] " + Path.GetFileName(file) + ": " + ex.Message); }
            }
            log?.Invoke($"[selftest] correct={correct}, wrong={wrong}, rejected={rejected}, errors={errors}.");
        }

        private static IEnumerable<string> EnumerateImages(string directory)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };
            return Directory.EnumerateFiles(directory).Where(path => extensions.Contains(Path.GetExtension(path)));
        }
    }
}
