using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.WorkCommands
{
    public sealed partial class AutoBestWorkCommandWorkflow
    {
        private static bool TryParseProfile(
            ConfirmedCommandSendResult result,
            out AutoBestWorkProfile profile)
        {
            profile = null;
            return result?.IsConfirmed == true &&
                AutoBestProfileParser.TryParse(CombineReplyText(result.ReplyMessage), out profile);
        }

        private static bool TryParseWorkerLevel(
            ConfirmedCommandSendResult result,
            out int workerLevel)
        {
            workerLevel = 0;
            return result?.IsConfirmed == true &&
                WorkerProfessionParser.TryParseLevel(CombineReplyText(result.ReplyMessage), out workerLevel);
        }

        private static bool TryParsePotions(
            ConfirmedCommandSendResult result,
            out bool fishPotionActive,
            out bool woodPotionActive)
        {
            fishPotionActive = false;
            woodPotionActive = false;
            return result?.IsConfirmed == true && ActiveWorkPotionParser.TryParse(
                CombineReplyText(result.ReplyMessage),
                out fishPotionActive,
                out woodPotionActive);
        }

        private static string CombineReplyText(DiscordMessageSnapshot reply)
        {
            return (reply?.RenderedText ?? string.Empty) + "\n" + (reply?.Text ?? string.Empty);
        }
    }
}
