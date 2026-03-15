using System;

namespace EpicRPGBot.UI.Automation
{
    public sealed class AutomationOptions
    {
        public bool IsEnabled { get; private set; }

        public int DebugPort { get; private set; }

        public string SessionId { get; private set; } = string.Empty;

        public static AutomationOptions Parse(string[] args)
        {
            var options = new AutomationOptions();

            for (var i = 0; i < (args?.Length ?? 0); i++)
            {
                var arg = args[i] ?? string.Empty;
                if (arg.Equals("--automation", StringComparison.OrdinalIgnoreCase))
                {
                    options.IsEnabled = true;
                }
                else if (arg.Equals("--automation-debug-port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    i++;
                    options.DebugPort = ParseInt(args[i]);
                }
                else if (arg.Equals("--automation-session", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    i++;
                    options.SessionId = args[i] ?? string.Empty;
                }
            }

            return options;
        }

        private static int ParseInt(string raw)
        {
            return int.TryParse(raw, out var value) ? value : 0;
        }
    }
}
