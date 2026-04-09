using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Dungeon;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Dungeon
{
    public sealed class DungeonWorkflowTests
    {
        [Fact]
        public async Task RunAsync_CompletesDungeonAndDeletesChannel()
        {
            var fileName = "dungeon-test-" + Guid.NewGuid().ToString("N") + ".ini";
            try
            {
                var settingsService = CreateSettingsService(fileName);
                var chatClient = new FakeDungeonChatClient();
                var workflow = CreateWorkflow(chatClient, settingsService);
                var reports = new List<string>();

                var result = await workflow.RunAsync(reports.Add, CancellationToken.None);

                Assert.True(result.Completed);
                Assert.Equal("Dungeon completed.", result.Summary);
                Assert.Equal(DungeonTestData.TestPlayerName, settingsService.Current.ProfilePlayerName);
                Assert.True(chatClient.DeleteClicked);
                Assert.Contains("rpg p", chatClient.SentCommands);
                Assert.Contains("rpg dung <@222>", chatClient.SentCommands);
                Assert.Contains("bite", chatClient.SentMessages);
            }
            finally
            {
                DeleteSettingsFile(fileName);
            }
        }

        [Fact]
        public async Task RunAsync_ClicksInviteAlreadyVisibleWhenItBelongsToCurrentSignup()
        {
            var fileName = "dungeon-test-" + Guid.NewGuid().ToString("N") + ".ini";
            try
            {
                var settingsService = CreateSettingsService(fileName);
                var chatClient = new FakeDungeonChatClient(inviteAlreadyVisible: true, useSnowflakeIds: true);
                var workflow = CreateWorkflow(chatClient, settingsService);

                var result = await workflow.RunAsync(_ => { }, CancellationToken.None);

                Assert.True(result.Completed);
                Assert.Contains("bite", chatClient.SentMessages);
            }
            finally
            {
                DeleteSettingsFile(fileName);
            }
        }

        [Fact]
        public async Task RunAsync_RetriesBusyPartnerAndWaitsForFreshInvite()
        {
            var fileName = "dungeon-test-" + Guid.NewGuid().ToString("N") + ".ini";
            try
            {
                var settingsService = CreateSettingsService(fileName);
                var chatClient = new FakeDungeonChatClient(
                    dungeonEntryReplies: new[]
                    {
                        "You can't enter a dungeon while you or one of your partners is in the middle of a command!",
                        "You can't enter a dungeon while you or one of your partners is in the middle of a command!",
                        "You can't enter a dungeon while you or one of your partners is in the middle of a command!"
                    });
                var reports = new List<string>();
                var workflow = CreateWorkflow(chatClient, settingsService);

                var result = await workflow.RunAsync(reports.Add, CancellationToken.None);

                Assert.True(result.Completed);
                Assert.Equal(4, CountCommand(chatClient.SentCommands, "rpg dung <@222>"));
                Assert.Equal(2, chatClient.TakeMeThereClickCount);
                Assert.Contains(reports, message => message.Contains("Waiting 5 seconds before retry 2/3", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(reports, message => message.Contains("Waiting for a fresh dungeon invite", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                DeleteSettingsFile(fileName);
            }
        }

        [Fact]
        public void DungeonLobbyParser_ResolvesNonSelfPartnerMention()
        {
            var parser = new DungeonLobbyParser();
            var snapshots = new[]
            {
                new DiscordMessageSnapshot(
                    "m1",
                    "Dungeon 6 commands",
                    "Army Helper",
                    "Dungeon 6 commands\nPlayers listed\n" + DungeonTestData.TestDisplayMention + " - " + DungeonTestData.TestPlayerName + "\n@partner - partner",
                    mentions: new[]
                    {
                        new DiscordMessageMention(DungeonTestData.TestDisplayMention, "111"),
                        new DiscordMessageMention("@partner", "222")
                    })
            };

            var partner = parser.FindPartnerMention(snapshots, DungeonTestData.TestPlayerName);

            Assert.NotNull(partner);
            Assert.Equal("222", partner.UserId);
        }

        [Fact]
        public void DungeonLobbyParser_UsesPlayersListedNameToSkipSelfDisplayMention()
        {
            var parser = new DungeonLobbyParser();
            var snapshots = new[]
            {
                new DiscordMessageSnapshot(
                    "m1",
                    "Dungeon 9 with eternal partner commands",
                    "Army Helper",
                    "@Jpack " + DungeonTestData.TestDisplayMention + " Your dungeon is ready.\nPlayers listed:\n@Jpack - jpack2552\n" + DungeonTestData.TestDisplayMention + " - " + DungeonTestData.TestPlayerName + "\nRecommended trades",
                    mentions: new[]
                    {
                        new DiscordMessageMention("@Jpack", "111"),
                        new DiscordMessageMention(DungeonTestData.TestDisplayMention, "222")
                    })
            };

            var partner = parser.FindPartnerMention(snapshots, DungeonTestData.TestPlayerName);

            Assert.NotNull(partner);
            Assert.Equal("@Jpack", partner.Label);
            Assert.Equal("111", partner.UserId);
        }

        [Fact]
        public void DungeonLobbyParser_FallsBackToPlayersListedMentionWhenIdsAreUnavailable()
        {
            var parser = new DungeonLobbyParser();
            var snapshots = new[]
            {
                new DiscordMessageSnapshot(
                    "m1",
                    "Dungeon 9 with eternal partner commands",
                    "Army Helper",
                    "@Jpack " + DungeonTestData.TestDisplayMention + " Your dungeon is ready.\nPlayers listed:\n@Jpack - jpack2552\n" + DungeonTestData.TestDisplayMention + " - " + DungeonTestData.TestPlayerName + "\nRecommended trades")
            };

            var partner = parser.FindPartnerMention(snapshots, DungeonTestData.TestPlayerName);

            Assert.NotNull(partner);
            Assert.Equal("@Jpack", partner.Label);
            Assert.Equal(string.Empty, partner.UserId);
        }

        [Fact]
        public void DungeonBattleStateParser_DetectsTurnVictoryAndDeletePrompt()
        {
            var parser = new DungeonBattleStateParser();
            var snapshots = new[]
            {
                new DiscordMessageSnapshot(
                    "m1",
                    "YOU HAVE ENCOUNTERED THE xd DRAGON\nit's " + DungeonTestData.TestPlayerName + "'s turn!",
                    "EPIC RPG",
                    "YOU HAVE ENCOUNTERED THE xd DRAGON\nit's " + DungeonTestData.TestPlayerName + "'s turn!",
                    new[] { new DiscordMessageButton("BITE", 0, 0) }),
                new DiscordMessageSnapshot(
                    "m2",
                    "THE xd DRAGON DIED, ALL PLAYERS WON",
                    "EPIC RPG",
                    "THE xd DRAGON DIED, ALL PLAYERS WON"),
                new DiscordMessageSnapshot(
                    "m3",
                    "Thanks for using our dungeon system.",
                    "Army Helper",
                    "Thanks for using our dungeon system.",
                    new[] { new DiscordMessageButton("Delete dungeon channel", 0, 0) })
            };

            var state = parser.Parse(snapshots, DungeonTestData.TestPlayerName);

            Assert.True(state.HasEncounter);
            Assert.True(state.ShouldBite);
            Assert.True(state.HasVictory);
            Assert.NotNull(state.DeletePrompt);
        }

        private static AppSettingsService CreateSettingsService(string fileName)
        {
            var service = new AppSettingsService(new LocalSettingsStore(fileName));
            service.Save(AppSettingsSnapshot.Default
                .WithChannelUrl("https://discord.com/channels/713541415099170836/1145038338630373407")
                .WithAutoDeleteDungeonChannel(true));
            return service;
        }

        private static DungeonWorkflow CreateWorkflow(FakeDungeonChatClient chatClient, AppSettingsService settingsService)
        {
            return new DungeonWorkflow(
                chatClient,
                new ConfirmedCommandSender(chatClient),
                settingsService,
                settingsService.LoadCurrent,
                new DungeonLobbyParser(),
                new DungeonBattleStateParser(),
                new DungeonInviteWatcher(chatClient),
                new DungeonEntryReplyParser(),
                (_, __) => Task.CompletedTask);
        }

        private static int CountCommand(IReadOnlyList<string> commands, string expectedCommand)
        {
            var count = 0;
            for (var i = 0; i < commands.Count; i++)
            {
                if (string.Equals(commands[i], expectedCommand, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        private static void DeleteSettingsFile(string fileName)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EpicRPGBot.UI",
                "settings",
                fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
