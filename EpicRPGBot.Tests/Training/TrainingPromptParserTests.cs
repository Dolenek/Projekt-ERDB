using System.Collections.Generic;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Training;
using Xunit;

namespace EpicRPGBot.Tests.Training
{
    public sealed class TrainingPromptParserTests
    {
        [Fact]
        public void Parse_CountQuestionForestLogs_ResolvesCorrectAnswer()
        {
            var parser = new TrainingPromptParser();
            var snapshot = new DiscordMessageSnapshot(
                "training-1",
                "firendr is training in the forest!",
                renderedText: "firendr is training in the forest!\n:MEGAlog::SUPERlog::SUPERlog::EPIClog::MEGAlog:\nHow many :SUPERlog: do you see? you have 15 seconds!",
                buttons: new List<DiscordMessageButton>
                {
                    new DiscordMessageButton("0", 0, 0),
                    new DiscordMessageButton("1", 0, 1),
                    new DiscordMessageButton("2", 0, 2),
                    new DiscordMessageButton("3", 1, 0),
                    new DiscordMessageButton("4", 1, 1),
                    new DiscordMessageButton("5", 1, 2)
                });

            var resolution = parser.Parse(snapshot);

            Assert.True(resolution.IsTrainingPrompt);
            Assert.True(resolution.IsResolved);
            Assert.Equal(TrainingPromptKind.CountQuestion, resolution.Kind);
            Assert.Equal("2", resolution.AnswerText);
            Assert.Equal("2", resolution.PreferredButtonLabel);
        }

        [Fact]
        public void Parse_YesNoQuestionWithInlineEmoji_ResolvesNo()
        {
            var parser = new TrainingPromptParser();
            var snapshot = new DiscordMessageSnapshot(
                "training-2",
                "firendr is training in the... casino?",
                renderedText: "firendr is training in the... casino?\nIs this a DICE ? :gem:\nAnswer with yes or no! You have 15 seconds!",
                buttons: new List<DiscordMessageButton>
                {
                    new DiscordMessageButton("yes", 0, 0),
                    new DiscordMessageButton("no", 0, 1)
                });

            var resolution = parser.Parse(snapshot);

            Assert.True(resolution.IsTrainingPrompt);
            Assert.True(resolution.IsResolved);
            Assert.Equal(TrainingPromptKind.YesNoMatch, resolution.Kind);
            Assert.Equal("no", resolution.AnswerText);
            Assert.Equal("no", resolution.PreferredButtonLabel);
        }

        [Fact]
        public void Parse_YesNoQuestionWithEmojiOnNextLine_ResolvesNo()
        {
            var parser = new TrainingPromptParser();
            var snapshot = new DiscordMessageSnapshot(
                "training-3",
                "firendr is training in the... casino?",
                renderedText: "firendr is training in the... casino?\nIs this a DICE ?\n:gem:\nAnswer with yes or no! You have 15 seconds!",
                buttons: new List<DiscordMessageButton>
                {
                    new DiscordMessageButton("yes", 0, 0),
                    new DiscordMessageButton("no", 0, 1)
                });

            var resolution = parser.Parse(snapshot);

            Assert.True(resolution.IsTrainingPrompt);
            Assert.True(resolution.IsResolved);
            Assert.Equal(TrainingPromptKind.YesNoMatch, resolution.Kind);
            Assert.Equal("no", resolution.AnswerText);
            Assert.Equal("no", resolution.PreferredButtonLabel);
        }

        [Fact]
        public void Parse_YesNoQuestionDiamondVersusGem_ResolvesYes()
        {
            var parser = new TrainingPromptParser();
            var snapshot = new DiscordMessageSnapshot(
                "training-4",
                "firendr is training in the... casino?",
                renderedText: "firendr is training in the... casino?\nIs this a DIAMOND ? :gem:\nAnswer with yes or no! You have 15 seconds!",
                buttons: new List<DiscordMessageButton>
                {
                    new DiscordMessageButton("yes", 0, 0),
                    new DiscordMessageButton("no", 0, 1)
                });

            var resolution = parser.Parse(snapshot);

            Assert.True(resolution.IsTrainingPrompt);
            Assert.True(resolution.IsResolved);
            Assert.Equal(TrainingPromptKind.YesNoMatch, resolution.Kind);
            Assert.Equal("yes", resolution.AnswerText);
            Assert.Equal("yes", resolution.PreferredButtonLabel);
        }
    }
}
