using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class ConsoleMessageNavigationRouterTests
{
    [Fact]
    public async Task NavigateAsync_SelectsMatchingTabAndNavigatesOnce()
    {
        var callOrder = new List<string>();
        var navigator = new RecordingNavigator(true, () => callOrder.Add("navigate"));
        var selections = 0;
        var router = new ConsoleMessageNavigationRouter(new[]
        {
            new NavigationRoute(DiscordTabRole.Bot, _ =>
            {
                selections++;
                callOrder.Add("select");
                return Task.CompletedTask;
            }, navigator)
        });
        var reference = CompleteReference(DiscordTabRole.Bot);

        var navigated = await router.NavigateAsync(reference);

        Assert.True(navigated);
        Assert.Equal(1, selections);
        Assert.Equal(1, navigator.CallCount);
        Assert.Same(reference, navigator.LastReference);
        Assert.Equal(new[] { "select", "navigate" }, callOrder);
    }

    [Fact]
    public async Task NavigateAsync_IgnoresUnregisteredAndIncompleteTargets()
    {
        var navigator = new RecordingNavigator(true);
        var selections = 0;
        var router = new ConsoleMessageNavigationRouter(new[]
        {
            new NavigationRoute(DiscordTabRole.Bot, _ =>
            {
                selections++;
                return Task.CompletedTask;
            }, navigator)
        });

        Assert.False(await router.NavigateAsync(CompleteReference(DiscordTabRole.Guild)));
        Assert.False(await router.NavigateAsync(new DiscordMessageReference("", DiscordTabRole.Bot, "")));
        Assert.Equal(0, selections);
        Assert.Equal(0, navigator.CallCount);
    }

    private static DiscordMessageReference CompleteReference(DiscordTabRole role) =>
        new(
            "chat-messages-333333333333333",
            role,
            "https://discord.com/channels/111111111111111/222222222222222");

    private sealed class RecordingNavigator : IDiscordMessageNavigator
    {
        private readonly bool _result;
        private readonly Action? _onNavigate;

        public RecordingNavigator(bool result, Action? onNavigate = null)
        {
            _result = result;
            _onNavigate = onNavigate;
        }

        public int CallCount { get; private set; }
        public DiscordMessageReference? LastReference { get; private set; }

        public Task<bool> NavigateToMessageAsync(
            DiscordMessageReference reference,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            _onNavigate?.Invoke();
            LastReference = reference;
            return Task.FromResult(_result);
        }
    }
}
