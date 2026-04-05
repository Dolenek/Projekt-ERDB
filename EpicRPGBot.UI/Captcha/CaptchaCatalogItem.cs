namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaCatalogItem
    {
        public string Name { get; set; }

        public string Outline { get; set; }

        public string GrayscaleCues { get; set; }

        public string Disambiguation { get; set; }

        public string BuildPromptLine(int oneBasedIndex)
        {
            return $"{oneBasedIndex}. {Name} | outline: {Outline} | grayscale: {GrayscaleCues} | distinguish: {Disambiguation}";
        }
    }
}
