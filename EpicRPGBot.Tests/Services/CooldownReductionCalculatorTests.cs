using System;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class CooldownReductionCalculatorTests
    {
        [Fact]
        public void Reduce_AppliesImmediateVisualReductionAcrossCooldownKinds()
        {
            var reduction = TimeSpan.FromMinutes(15);

            Assert.Equal(
                TimeSpan.FromMinutes(30),
                CooldownReductionCalculator.Reduce(TimeSpan.FromMinutes(45), reduction));
            Assert.Null(CooldownReductionCalculator.Reduce(TimeSpan.FromMinutes(10), reduction));
            Assert.Null(CooldownReductionCalculator.Reduce(TimeSpan.FromMinutes(5), reduction));
            Assert.Equal(
                TimeSpan.FromMinutes(1),
                CooldownReductionCalculator.Reduce(TimeSpan.FromMinutes(16), reduction));
            Assert.Null(CooldownReductionCalculator.Reduce(null, reduction));
        }
    }
}
