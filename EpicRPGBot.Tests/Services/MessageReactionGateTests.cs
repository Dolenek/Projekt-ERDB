using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class MessageReactionGateTests
    {
        [Fact]
        public void MessageId_IsAcceptedOnlyOnceUntilReset()
        {
            var gate = new MessageReactionGate();

            Assert.True(gate.TryBegin("m1"));
            Assert.False(gate.TryBegin("m1"));

            gate.Reset();

            Assert.True(gate.TryBegin("m1"));
        }

        [Fact]
        public void BlankMessageId_DoesNotBlockHandling()
        {
            var gate = new MessageReactionGate();

            Assert.True(gate.TryBegin(string.Empty));
            Assert.True(gate.TryBegin(string.Empty));
        }
    }
}
