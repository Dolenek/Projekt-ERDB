using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class GuildRaidTriggerProcessorTests
    {
        [Fact]
        public void ContainsMatch_TriggersCaseInsensitively()
        {
            var settings = CreateSettings(triggerText: "guild raid");
            var snapshot = CreateSnapshot("m1", "Time to raid guild raid now");

            var matched = GuildRaidTriggerMatcher.IsMatch(settings, snapshot);

            Assert.True(matched);
        }

        [Fact]
        public void ExactMatch_RequiresTrimmedFullMessageMatch()
        {
            var settings = CreateSettings(triggerText: "guild raid", matchMode: GuildRaidMatchModes.Exact);
            var exactSnapshot = CreateSnapshot("m1", "  guild raid  ");
            var containsOnlySnapshot = CreateSnapshot("m2", "Time for guild raid");

            Assert.True(GuildRaidTriggerMatcher.IsMatch(settings, exactSnapshot));
            Assert.False(GuildRaidTriggerMatcher.IsMatch(settings, containsOnlySnapshot));
        }

        [Fact]
        public void AuthorFilter_IsOptionalAndCaseInsensitive()
        {
            var withAuthorFilter = CreateSettings(triggerText: "guild raid", authorFilter: "army helper");
            var matchingAuthorSnapshot = CreateSnapshot("m1", "guild raid", "Army Helper");
            var otherAuthorSnapshot = CreateSnapshot("m2", "guild raid", "Somebody Else");

            Assert.True(GuildRaidTriggerMatcher.IsMatch(withAuthorFilter, matchingAuthorSnapshot));
            Assert.False(GuildRaidTriggerMatcher.IsMatch(withAuthorFilter, otherAuthorSnapshot));
            Assert.True(GuildRaidTriggerMatcher.IsMatch(CreateSettings(triggerText: "guild raid"), otherAuthorSnapshot));
        }

        [Fact]
        public void IncompleteOrInvalidConfiguration_DoesNotMatch()
        {
            var incompleteSettings = CreateSettings(channelUrl: string.Empty);
            var invalidUrlSettings = CreateSettings(channelUrl: "https://discord.com/app");
            var snapshot = CreateSnapshot("m1", "guild raid");

            Assert.False(GuildRaidTriggerMatcher.IsMatch(incompleteSettings, snapshot));
            Assert.False(invalidUrlSettings.TryResolveGuildRaidChannelUrl(out _));
            Assert.False(GuildRaidTriggerMatcher.IsMatch(invalidUrlSettings, snapshot));
        }

        [Fact]
        public void Processor_DedupesByMessageId()
        {
            var processor = new GuildRaidTriggerProcessor();
            var settings = CreateSettings(triggerText: "guild raid");
            var snapshot = CreateSnapshot("m1", "guild raid");

            Assert.True(processor.ShouldTrigger(settings, snapshot));
            Assert.False(processor.ShouldTrigger(settings, snapshot));
        }

        private static AppSettingsSnapshot CreateSettings(
            string channelUrl = "https://discord.com/channels/1/2",
            string triggerText = "guild raid",
            string matchMode = GuildRaidMatchModes.Contains,
            string authorFilter = "")
        {
            return AppSettingsSnapshot.Default
                .WithGuildRaidChannelUrl(channelUrl)
                .WithGuildRaidTriggerText(triggerText)
                .WithGuildRaidMatchMode(matchMode)
                .WithGuildRaidAuthorFilter(authorFilter);
        }

        private static DiscordMessageSnapshot CreateSnapshot(string id, string renderedText, string author = "")
        {
            return new DiscordMessageSnapshot(id, renderedText, author, renderedText);
        }
    }
}
