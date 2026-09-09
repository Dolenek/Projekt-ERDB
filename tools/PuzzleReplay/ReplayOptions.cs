using System;
using System.IO;

namespace PuzzleReplay
{
    internal sealed class ReplayOptions
    {
        public string Root { get; private set; }
        public string Manifest { get; private set; }
        public string Output { get; private set; }
        public string PolicyPath { get; private set; }
        public bool Validate { get; private set; }
        public bool AllowUnsupported { get; private set; }

        public static ReplayOptions Parse(string[] args)
        {
            if (args.Length < 3) throw new ArgumentException("Expected repository, manifest and output paths.");
            var options = new ReplayOptions
            {
                Root = Path.GetFullPath(args[0]), Manifest = Path.GetFullPath(args[1]),
                Output = Path.GetFullPath(args[2]), PolicyPath = Path.Combine(Path.GetFullPath(args[0]), "puzzle-local.json")
            };
            for (var index = 3; index < args.Length; index++)
            {
                if (args[index] == "--validate") options.Validate = true;
                else if (args[index] == "--allow-unsupported") options.AllowUnsupported = true;
                else if (args[index] == "--policy" && index + 1 < args.Length)
                    options.PolicyPath = Path.GetFullPath(args[++index]);
                else throw new ArgumentException("Unknown or incomplete argument: " + args[index]);
            }
            if (options.Validate && options.AllowUnsupported)
                throw new ArgumentException("Unsupported-target diagnostics cannot seal a validation policy.");
            if (string.Equals(options.Output, options.PolicyPath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options.Output, options.Manifest, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output must not overwrite the policy or manifest.");
            return options;
        }
    }
}
