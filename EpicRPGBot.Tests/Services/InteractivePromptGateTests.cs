using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class InteractivePromptGateTests
    {
        [Fact]
        public void BunnyPending_StaysSetUntilEnded()
        {
            var gate = new InteractivePromptGate();

            Assert.True(gate.TryBeginBunny());
            Assert.False(gate.TryBeginBunny());
            Assert.True(gate.IsAnyPending);

            gate.EndBunny();

            Assert.False(gate.IsAnyPending);
        }

        [Fact]
        public void TrainingAndBunnyPending_AreTrackedIndependently()
        {
            var gate = new InteractivePromptGate();

            Assert.True(gate.TryBeginTraining());
            Assert.True(gate.TryBeginBunny());
            Assert.True(gate.IsAnyPending);

            gate.EndBunny();
            Assert.True(gate.IsAnyPending);

            gate.EndTraining();
            Assert.False(gate.IsAnyPending);
        }
    }
}
