using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Pets
{
    public static class PetReplyIdentity
    {
        public static bool IsEpic(DiscordMessageSnapshot reply) => DiscordMessageIdentity.IsEpic(reply);
    }
}
