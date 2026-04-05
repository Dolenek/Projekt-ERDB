using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EpicRPGBot.UI.Captcha;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaSelfTestRunner
    {
        public async Task RunAsync(Action<string> logInfo)
        {
            await Task.Yield();

            try
            {
                var settings = CaptchaSettings.LoadDefault();
                await RunReplaySelfTestAsync(settings, logInfo);
            }
            catch (Exception ex)
            {
                logInfo?.Invoke("[selftest] Error: " + ex.Message);
            }
        }

        private static async Task RunReplaySelfTestAsync(CaptchaSettings settings, Action<string> logInfo)
        {
            await Task.Yield();

            if (string.IsNullOrWhiteSpace(settings.SelfTestReplayDirectory))
            {
                logInfo?.Invoke("[selftest] CAPTCHA_SELFTEST_REPLAY_DIR is required for OpenAI replay self-test.");
                return;
            }

            if (!Directory.Exists(settings.SelfTestReplayDirectory))
            {
                logInfo?.Invoke($"[selftest] Replay dir not found: {settings.SelfTestReplayDirectory}");
                return;
            }

            var catalog = CaptchaItemCatalog.Load(settings.ItemNamesFile);
            var provider = new CaptchaProviderFactory().Create(settings);
            var files = EnumerateImageFiles(settings.SelfTestReplayDirectory);
            var passed = 0;
            var evaluated = 0;

            logInfo?.Invoke($"[selftest] Found {files.Count} replay images in {settings.SelfTestReplayDirectory}.");

            foreach (var file in files)
            {
                if (!catalog.TryResolveExpectedLabelFromFileName(file, out var expected))
                {
                    logInfo?.Invoke($"[selftest] Skipping {Path.GetFileName(file)}: filename does not map to a catalog item.");
                    continue;
                }

                try
                {
                    evaluated++;
                    var result = await provider.SolveAsync(File.ReadAllBytes(file), default);
                    var predicted = result.IsMatch ? result.Label : "<none>";
                    var outcome = result.IsMatch && string.Equals(result.Label, expected, StringComparison.OrdinalIgnoreCase)
                        ? "PASS"
                        : "FAIL";

                    if (outcome == "PASS")
                    {
                        passed++;
                    }

                    logInfo?.Invoke($"[selftest] {Path.GetFileName(file)} => expected='{expected}', predicted='{predicted}', outcome={outcome}, detail={result.Detail}");
                }
                catch (Exception ex)
                {
                    evaluated++;
                    logInfo?.Invoke($"[selftest] {Path.GetFileName(file)} => expected='{expected}', predicted='<error>', outcome=FAIL, detail={ex.Message}");
                }
            }

            logInfo?.Invoke($"[selftest] Replay summary: {passed}/{evaluated} passed.");
        }

        private static List<string> EnumerateImageFiles(string directory)
        {
            var files = new List<string>();
            files.AddRange(Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly));
            files.AddRange(Directory.EnumerateFiles(directory, "*.jpg", SearchOption.TopDirectoryOnly));
            files.AddRange(Directory.EnumerateFiles(directory, "*.jpeg", SearchOption.TopDirectoryOnly));
            return files;
        }
    }
}
