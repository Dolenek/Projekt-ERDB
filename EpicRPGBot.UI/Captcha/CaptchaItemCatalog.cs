using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaItemCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly List<CaptchaCatalogItem> _items;
        private readonly Dictionary<string, string> _itemsByName;

        private CaptchaItemCatalog(List<CaptchaCatalogItem> items)
        {
            _items = items;
            _itemsByName = items.ToDictionary(item => item.Name, item => item.Name, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<CaptchaCatalogItem> Items => _items;

        public int Count => _items.Count;

        public static CaptchaItemCatalog Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("CAPTCHA_ITEM_NAMES_FILE is required for OpenAI captcha solving.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Captcha item names file was not found.", path);
            }

            var items = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? LoadJson(path)
                : LoadText(path);
            if (items.Count == 0)
            {
                throw new InvalidOperationException("Captcha item names file does not contain any usable item names.");
            }

            return new CaptchaItemCatalog(items);
        }

        public string GetItemName(int oneBasedIndex)
        {
            var item = GetItem(oneBasedIndex);
            return item?.Name ?? string.Empty;
        }

        public CaptchaCatalogItem GetItem(int oneBasedIndex)
        {
            if (oneBasedIndex < 1 || oneBasedIndex > _items.Count)
            {
                return null;
            }

            return _items[oneBasedIndex - 1];
        }

        public bool TryResolveExpectedLabelFromFileName(string filePath, out string expectedLabel)
        {
            var stem = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
            var exact = stem.Trim();
            if (_itemsByName.TryGetValue(exact, out expectedLabel))
            {
                return true;
            }

            var dividerIndex = exact.IndexOf("__", StringComparison.Ordinal);
            if (dividerIndex <= 0)
            {
                expectedLabel = string.Empty;
                return false;
            }

            var candidate = exact.Substring(0, dividerIndex).Trim();
            if (_itemsByName.TryGetValue(candidate, out expectedLabel))
            {
                return true;
            }

            expectedLabel = string.Empty;
            return false;
        }

        public string BuildNumberedList()
        {
            return string.Join(Environment.NewLine, _items.Select((item, index) => item.BuildPromptLine(index + 1)));
        }

        private static List<CaptchaCatalogItem> LoadText(string path)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<CaptchaCatalogItem>();

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (seen.Add(line))
                {
                    items.Add(new CaptchaCatalogItem
                    {
                        Name = line,
                        Outline = "No outline description provided.",
                        GrayscaleCues = "No grayscale cues provided.",
                        Disambiguation = "No disambiguation notes provided."
                    });
                }
            }

            return items;
        }

        private static List<CaptchaCatalogItem> LoadJson(string path)
        {
            var parsed = JsonSerializer.Deserialize<List<CaptchaCatalogItem>>(File.ReadAllText(path), JsonOptions);
            if (parsed == null)
            {
                return new List<CaptchaCatalogItem>();
            }

            return parsed
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                .Select(item => new CaptchaCatalogItem
                {
                    Name = item.Name.Trim(),
                    Outline = CleanField(item.Outline),
                    GrayscaleCues = CleanField(item.GrayscaleCues),
                    Disambiguation = CleanField(item.Disambiguation)
                })
                .ToList();
        }

        private static string CleanField(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None." : value.Trim();
        }
    }
}
