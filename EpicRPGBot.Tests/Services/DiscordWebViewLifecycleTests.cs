using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class DiscordWebViewLifecycleTests
{
    [Fact]
    public async Task CreatesOnlyOnDemandAndSharesOneInstanceAcrossReasons()
    {
        var harness = new LifecycleHarness();

        Assert.Equal(0, harness.CreateCount);
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Permanent, true);
        var firstInstance = harness.Lifecycle.Current;
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, true);

        Assert.Same(firstInstance, harness.Lifecycle.Current);
        Assert.Equal(1, harness.CreateCount);

        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, false);
        Assert.Same(firstInstance, harness.Lifecycle.Current);
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Permanent, false);
        Assert.Null(harness.Lifecycle.Current);
        Assert.Equal(1, firstInstance!.DisposeCount);
    }

    [Fact]
    public async Task WorkflowLeaseKeepsInstanceAliveAfterTabSwitch()
    {
        var harness = new LifecycleHarness();
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, true);
        await harness.Lifecycle.AcquireAsync(DiscordWebViewActivityReason.Workflow);
        var webView = harness.Lifecycle.Current;

        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, false);
        Assert.Same(webView, harness.Lifecycle.Current);

        await harness.Lifecycle.ReleaseAsync(DiscordWebViewActivityReason.Workflow);
        Assert.Null(harness.Lifecycle.Current);
        Assert.Equal(1, webView!.DisposeCount);
    }

    [Fact]
    public async Task LastReleaseDisposesOnceAndNextDemandRestoresUrl()
    {
        var harness = new LifecycleHarness();
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, true);
        var firstInstance = harness.Lifecycle.Current!;
        firstInstance.CurrentUrl = "https://discord.com/channels/123/789";

        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, false);
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, false);
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, true);

        Assert.Equal(1, firstInstance.DisposeCount);
        Assert.NotSame(firstInstance, harness.Lifecycle.Current);
        Assert.Equal(2, harness.CreateCount);
        Assert.Equal("https://discord.com/channels/123/789", harness.InitializedUrls[1]);
    }

    [Fact]
    public async Task ConcurrentActivationCreatesOneInstance()
    {
        var initializationStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var continueInitialization = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var harness = new LifecycleHarness(async (webView, url, cancellationToken) =>
        {
            initializationStarted.TrySetResult(true);
            await continueInitialization.Task.WaitAsync(cancellationToken);
            webView.IsReady = true;
        });

        var permanentActivation = harness.Lifecycle.SetDemandAsync(
            DiscordWebViewActivityReason.Permanent,
            true);
        await initializationStarted.Task;
        var selectedActivation = harness.Lifecycle.SetDemandAsync(
            DiscordWebViewActivityReason.Selected,
            true);
        continueInitialization.TrySetResult(true);
        await Task.WhenAll(permanentActivation, selectedActivation);

        Assert.Equal(1, harness.CreateCount);
    }

    [Fact]
    public async Task FailedInitializationIsDisposedAndCanBeRetried()
    {
        var attempt = 0;
        var harness = new LifecycleHarness((webView, url, cancellationToken) =>
        {
            attempt++;
            if (attempt == 1)
            {
                throw new InvalidOperationException("failed initialization");
            }

            webView.IsReady = true;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, true));
        var failedInstance = harness.CreatedInstances[0];

        Assert.Null(harness.Lifecycle.Current);
        Assert.Equal(1, failedInstance.DisposeCount);
        await harness.Lifecycle.SetDemandAsync(DiscordWebViewActivityReason.Selected, true);
        Assert.True(harness.Lifecycle.Current!.IsReady);
        Assert.Equal(2, harness.CreateCount);
    }

    private sealed class LifecycleHarness
    {
        private const string InitialUrl = "https://discord.com/channels/123/456";
        private readonly Func<FakeWebView, string, CancellationToken, Task> _initializeAsync;

        public LifecycleHarness(
            Func<FakeWebView, string, CancellationToken, Task>? initializeAsync = null)
        {
            _initializeAsync = initializeAsync ?? InitializeSuccessfullyAsync;
            Lifecycle = new DiscordWebViewLifecycle<FakeWebView>(
                Create,
                InitializeAsync,
                webView => webView.IsReady,
                webView => webView.CurrentUrl,
                webView => webView.Dispose(),
                () => InitialUrl);
        }

        public DiscordWebViewLifecycle<FakeWebView> Lifecycle { get; }

        public List<FakeWebView> CreatedInstances { get; } = new();

        public List<string> InitializedUrls { get; } = new();

        public int CreateCount => CreatedInstances.Count;

        private FakeWebView Create()
        {
            var webView = new FakeWebView();
            CreatedInstances.Add(webView);
            return webView;
        }

        private async Task InitializeAsync(
            FakeWebView webView,
            string url,
            CancellationToken cancellationToken)
        {
            InitializedUrls.Add(url);
            await _initializeAsync(webView, url, cancellationToken);
        }

        private static Task InitializeSuccessfullyAsync(
            FakeWebView webView,
            string url,
            CancellationToken cancellationToken)
        {
            webView.CurrentUrl = url;
            webView.IsReady = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWebView : IDisposable
    {
        public string CurrentUrl { get; set; } = string.Empty;

        public bool IsReady { get; set; }

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            IsReady = false;
        }
    }
}
