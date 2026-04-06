using System.Text;

namespace EpicRPGBot.UI.Captcha
{
    public static class CaptchaVisionPromptBuilder
    {
        public static string BuildSystemPrompt()
        {
            return "You solve Epic RPG guard item prompts. " +
                   "The image contains a single target item icon. " +
                   "Ignore decorative diagonal lines, surrounding UI, and unrelated text. " +
                   "If the image is in color, use color as a strong signal together with shape. " +
                   "Only fall back to grayscale cues when the captcha is clearly grayscale, desaturated, or black-and-white. " +
                   "For full-color images, do not ignore obvious color evidence. " +
                   "Select exactly one item from the provided catalog or return unknown if the image is too ambiguous.";
        }

        public static string BuildUserPrompt(CaptchaItemCatalog catalog, bool enhancedRetryImage)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Identify the Epic RPG item shown in the image.");
            builder.AppendLine("Return the best match from the numbered catalog.");
            builder.AppendLine("Use color first when the image is clearly colored.");
            builder.AppendLine("Use the outline, grayscale, and disambiguation notes to separate similar icons.");
            builder.AppendLine("If a colored image strongly matches one item by color, do not prefer a grayscale-only silhouette match from another item.");
            builder.AppendLine("If the image is too unclear, return unknown with item_index = 0.");
            if (enhancedRetryImage)
            {
                builder.AppendLine("This retry image is an enlarged version of the same source capture.");
            }

            builder.AppendLine();
            builder.AppendLine("Catalog:");
            builder.Append(catalog.BuildNumberedList());
            return builder.ToString();
        }
    }
}
