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
    string Value,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record TextReadResult(
    string AutomationId,
    string Value,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record ListReadResult(
    string AutomationId,
    IReadOnlyList<string> Items,
    int ItemCount = 0,
    string SelectedTab = "",
    bool ControlFound = true,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record ImageArtifactResult(
    string ImagePath,
    int Width,
    int Height,
    string Source,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record WebViewEvalResult(
    string TargetUrl,
    string TargetTitle,
    string JsonValue,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record WaitForTextResult(
    string AutomationId,
    string Value,
    bool Matched,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record WebViewDebugStateResult(
    string TargetUrl,
    string TargetTitle,
    string ReadyState,
    string TabRole,
    string BodyPreview,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record WebViewMessageSnapshot(
    string Id,
    string Author,
    string Text);

public sealed record WebViewMessagesResult(
    string TargetUrl,
    string TargetTitle,
    IReadOnlyList<WebViewMessageSnapshot> Messages,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);

public sealed record WebViewWaitResult(
    string TargetUrl,
    string TargetTitle,
    string FilterAuthor,
    string FilterText,
    string AfterId,
    bool Matched,
    bool TimedOut,
    WebViewMessageSnapshot? Message = null,
    bool Success = true,
    string Error = "",
    AppStatusResult? Status = null);
