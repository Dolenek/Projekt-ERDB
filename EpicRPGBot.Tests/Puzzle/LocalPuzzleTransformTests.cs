using EpicRPGBot.UI.Puzzle;
using EpicRPGBot.UI.Puzzle.Local;
using OpenCvSharp;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class LocalPuzzleTransformTests
{
    [WindowsFact]
    public async Task SmallCompactCalibrationItem_RefinementRecoversAnswerWithoutValidation()
    {
        var root = RepositoryRoot();
        var catalog = PuzzleItemCatalog.Load(Path.Combine(root, "items.json"));
        var policy = LocalPuzzlePolicy.Load(Path.Combine(root, "tools/puzzle/legacy-policy.json"));
        var attachment = File.ReadAllBytes(Path.Combine(root,
            "artifacts/puzzle-dataset/images/1491830499985850439.png"));
        using (var baseline = new LocalPuzzleAnswerProvider(Path.Combine(root, "Items"), catalog, policy))
            Assert.False((await baseline.SolveAsync(attachment, default)).IsMatch);
        policy.Pipeline = LocalPuzzlePolicy.RefinedPipeline;
        using var refined = new LocalPuzzleAnswerProvider(Path.Combine(root, "Items"), catalog, policy);
        var result = await refined.SolveAsync(attachment, default);
        Assert.Equal("epic fish", result.Label);
        Assert.True(result.IsMatch);
        Assert.False(result.AutomaticSubmissionAllowed);
    }

    [WindowsFact]
    public async Task TranslatedScaledAndGrayscaleCards_KeepCanonicalAnswer()
    {
        var root = RepositoryRoot();
        using var provider = new LocalPuzzleAnswerProvider(Path.Combine(root, "Items"),
            PuzzleItemCatalog.Load(Path.Combine(root, "items.json")),
            LocalPuzzlePolicy.Load(Path.Combine(root, "puzzle-local.json")));
        foreach (var scale in new[] { 0.8, 1.2 })
            foreach (var shift in new[] { -10, 10 })
                foreach (var grayscale in new[] { false, true })
                {
                    var attachment = CreateCard(root, scale, shift, grayscale);
                    var result = await provider.SolveAsync(attachment, default);
                    Assert.True(result.IsMatch, $"scale={scale}, shift={shift}, grayscale={grayscale}: {result.Detail}");
                    Assert.Equal("life potion", result.Label);
                }
    }

    [WindowsFact]
    public void ColorAgreement_DistinguishesWrongHueAndNeutralGrayscale()
    {
        using var red = new Mat(12, 12, MatType.CV_8UC3, new Scalar(30, 30, 200));
        using var blue = new Mat(12, 12, MatType.CV_8UC3, new Scalar(200, 30, 30));
        using var grayscale = new Mat(12, 12, MatType.CV_8UC3, Scalar.All(100));
        using var variant = new PuzzleTemplateVariant("apple", red);
        Assert.True(variant.ColorSimilarity(red, new Point()) > 0.99);
        Assert.Equal(0, variant.ColorSimilarity(blue, new Point()));
        Assert.Equal(1, variant.ColorSimilarity(grayscale, new Point()));
    }

    private static byte[] CreateCard(string root, double scale, int shift, bool grayscale)
    {
        using var original = PuzzleTemplateTransforms.ReadTemplate(Path.Combine(root, "Items/life potion.webp"));
        using var icon = PuzzleTemplateTransforms.Transform(original, 0, 48, 1);
        using var card = new Mat(200, 500, MatType.CV_8UC3, Scalar.All(34));
        using (var destination = new Mat(card, new Rect(40 + shift, 75 + shift, icon.Width, icon.Height)))
            icon.CopyTo(destination);
        Cv2.PutText(card, "what is this item?", new Point(145, 100),
            HersheyFonts.HersheySimplex, 0.8, Scalar.White, 2);
        if (grayscale)
        {
            using var gray = new Mat();
            Cv2.CvtColor(card, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(gray, card, ColorConversionCodes.GRAY2BGR);
        }
        using var resized = new Mat();
        Cv2.Resize(card, resized, new Size(), scale, scale, InterpolationFlags.Linear);
        Cv2.ImEncode(".png", resized, out var bytes);
        return bytes;
    }

    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "EpicRPGBotCSharp.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root");
    }
}
