using System;
using System.IO;
using System.Threading;
using EpicRPGBot.UI.Captcha;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: SmokeTest <repoRoot> <imagePath>");
            return 2;
        }

        try
        {
            var repoRoot = args[0];
            var imagePath = args[1];
            LoadCaptchaEnvironment(Path.Combine(repoRoot, ".env"), repoRoot);

            var settings = CaptchaSettings.LoadDefault();
            var provider = new CaptchaProviderFactory().Create(settings);
            var bytes = File.ReadAllBytes(imagePath);
            var result = provider.SolveAsync(bytes, CancellationToken.None).GetAwaiter().GetResult();

            Console.WriteLine("is_match=" + result.IsMatch);
            Console.WriteLine("label=" + result.Label);
            Console.WriteLine("method=" + result.Method);
            Console.WriteLine("detail=" + result.Detail);
            Console.WriteLine("items_file=" + settings.ItemNamesFile);
            Console.WriteLine("model=" + settings.OpenAiModel);
            Console.WriteLine("retry_model=" + settings.OpenAiRetryModel);
            return result.IsMatch ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static void LoadCaptchaEnvironment(string envPath, string repoRoot)
    {
        foreach (var raw in File.ReadAllLines(envPath))
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var index = line.IndexOf('=');
            if (index <= 0)
            {
                continue;
            }

            var key = line.Substring(0, index).Trim();
            if (!key.StartsWith("CAPTCHA_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = line.Substring(index + 1).Trim();
            if ((value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal)) ||
                (value.StartsWith("'", StringComparison.Ordinal) && value.EndsWith("'", StringComparison.Ordinal)))
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (string.Equals(key, "CAPTCHA_ITEM_NAMES_FILE", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(value) &&
                !Path.IsPathRooted(value))
            {
                value = Path.Combine(repoRoot, value);
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
