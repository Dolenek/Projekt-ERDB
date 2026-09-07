using EpicRPGBot.UI.Captcha;
using EpicRPGBot.UI.Captcha.Local;
using Xunit;

namespace EpicRPGBot.Tests.Captcha;

public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "OpenCvSharp Windows native runtime test.";
    }
}

public sealed class LocalCaptchaImageTests
{
    [WindowsFact]
    public async Task CorruptAndIconOnlyInputs_AreRejected()
    {
        var root = RepositoryRoot();
        using var provider = Create(root);
        Assert.False((await provider.SolveAsync(new byte[] { 1, 2, 3 }, default)).IsMatch);
        Assert.False((await provider.SolveAsync(File.ReadAllBytes(Path.Combine(root, "Items/apple.webp")), default)).IsMatch);
    }

    [WindowsFact]
    public async Task CancelledRequest_DoesNotProduceAnAnswer()
    {
        using var provider = Create(RepositoryRoot());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SolveAsync(new byte[] { 1 }, cancellation.Token));
    }

    [WindowsFact]
    public void MissingTemplates_PreventInitialization()
    {
        var root = RepositoryRoot();
        Assert.ThrowsAny<IOException>(() => new LocalCaptchaAnswerProvider(
            Path.Combine(root, "not-present"), CaptchaItemCatalog.Load(Path.Combine(root, "items.json")), new LocalCaptchaPolicy()));
    }

    [WindowsFact]
    public async Task BlankAndTextOnlyCards_AreRejected()
    {
        using var provider = Create(RepositoryRoot());
        using var image = new OpenCvSharp.Mat(200, 500, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(34));
        OpenCvSharp.Cv2.ImEncode(".png", image, out var blank);
        Assert.False((await provider.SolveAsync(blank, default)).IsMatch);
        OpenCvSharp.Cv2.PutText(image, "NO ITEM", new OpenCvSharp.Point(5, 100),
            OpenCvSharp.HersheyFonts.HersheySimplex, 0.6, OpenCvSharp.Scalar.White, 2);
        OpenCvSharp.Cv2.ImEncode(".png", image, out var textOnly);
        Assert.False((await provider.SolveAsync(textOnly, default)).IsMatch);
    }

    private static LocalCaptchaAnswerProvider Create(string root) => new(
        Path.Combine(root, "Items"), CaptchaItemCatalog.Load(Path.Combine(root, "items.json")),
        LocalCaptchaPolicy.Load(Path.Combine(root, "captcha-local.json")));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "EpicRPGBotCSharp.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root");
    }
}
