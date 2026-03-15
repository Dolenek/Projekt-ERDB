using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace EpicRPGBot.Mcp.Services;

public sealed class ArtifactStore
{
    private readonly string _artifactDirectory;

    public ArtifactStore()
    {
        _artifactDirectory = Path.Combine(Path.GetTempPath(), "EpicRPGBot.Mcp", "artifacts");
        Directory.CreateDirectory(_artifactDirectory);
    }

    public string SaveBitmap(string prefix, Bitmap bitmap)
    {
        var path = NextPath(prefix, ".png");
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    public string SaveBytes(string prefix, string extension, byte[] bytes)
    {
        var path = NextPath(prefix, extension);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string NextPath(string prefix, string extension)
    {
        var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "artifact" : prefix.Trim();
        return Path.Combine(_artifactDirectory, $"{safePrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}{extension}");
    }
}
