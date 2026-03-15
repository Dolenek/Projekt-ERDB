namespace EpicRPGBot.Mcp.Models;

public sealed record AppStatusResult(
    bool IsRunning,
    int ProcessId,
    int DebugPort,
    string SessionId,
    string WindowTitle,
    string ExecutablePath);

public sealed record CloseAppResult(bool Closed, int ExitCode);

public sealed record ControlSnapshot(
    string AutomationId,
    string Name,
    string ControlType,
    bool IsEnabled);

public sealed record ControlActionResult(
    string AutomationId,
    string Action,
    string Value);

public sealed record TextReadResult(
    string AutomationId,
    string Value);

public sealed record ListReadResult(
    string AutomationId,
    IReadOnlyList<string> Items);

public sealed record ImageArtifactResult(
    string ImagePath,
    int Width,
    int Height,
    string Source);

public sealed record WebViewEvalResult(
    string TargetUrl,
    string TargetTitle,
    string JsonValue);
