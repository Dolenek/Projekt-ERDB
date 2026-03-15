using EpicRPGBot.Mcp.Models;
using System.Drawing;
using System.Linq;
using System.Windows.Automation;

namespace EpicRPGBot.Mcp.Services;

public sealed class UiAutomationFacade
{
    private readonly ArtifactStore _artifacts;
    private readonly AutomationElementInspector _inspector;
    private readonly UiAppSession _session;

    public UiAutomationFacade(UiAppSession session, ArtifactStore artifacts, AutomationElementInspector inspector)
    {
        _session = session;
        _artifacts = artifacts;
        _inspector = inspector;
    }

    public AppStatusResult BringToFront()
    {
        _session.BringToFront();
        return _session.GetStatus();
    }

    public ImageArtifactResult CaptureWindow()
    {
        _session.BringToFront();
        var handle = _session.RequireWindowHandle();
        if (!NativeMethods.GetWindowRect(handle, out var rect))
        {
            throw new InvalidOperationException("Could not read EpicRPGBot.UI window bounds.");
        }

        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);

        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
        }

        var path = _artifacts.SaveBitmap("window", bitmap);
        return new ImageArtifactResult(path, width, height, "window");
    }

    public IReadOnlyList<ControlSnapshot> ListControls()
    {
        var window = GetWindowElement();
        var elements = window.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        var controls = new Dictionary<string, ControlSnapshot>(StringComparer.Ordinal);

        foreach (AutomationElement element in elements)
        {
            var automationId = element.Current.AutomationId;
            if (string.IsNullOrWhiteSpace(automationId) || controls.ContainsKey(automationId))
            {
                continue;
            }

            controls[automationId] = new ControlSnapshot(
                automationId,
                element.Current.Name ?? string.Empty,
                _inspector.FriendlyControlType(element.Current.ControlType),
                element.Current.IsEnabled);
        }

        return controls.Values.OrderBy(item => item.AutomationId).ToArray();
    }

    public ControlActionResult ClickControl(string automationId)
    {
        var element = FindControl(automationId);
        _session.BringToFront();

        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            ((InvokePattern)invokePattern).Invoke();
            return new ControlActionResult(automationId, "click", string.Empty);
        }

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
        {
            ((SelectionItemPattern)selectionPattern).Select();
            return new ControlActionResult(automationId, "click", string.Empty);
        }

        ClickByBounds(element);
        return new ControlActionResult(automationId, "click", string.Empty);
    }

    public ControlActionResult SetText(string automationId, string value)
    {
        var element = FindControl(automationId);
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
        {
            ((ValuePattern)valuePattern).SetValue(value ?? string.Empty);
            return new ControlActionResult(automationId, "set_text", value ?? string.Empty);
        }

        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern) &&
            bool.TryParse(value, out var toggleValue))
        {
            var toggle = (TogglePattern)togglePattern;
            var desired = toggleValue ? ToggleState.On : ToggleState.Off;
            if (toggle.Current.ToggleState != desired)
            {
                toggle.Toggle();
            }

            return new ControlActionResult(automationId, "set_text", toggleValue.ToString());
        }

        throw new InvalidOperationException($"Control '{automationId}' does not support text entry.");
    }

    public TextReadResult GetText(string automationId)
    {
        var element = FindControl(automationId);
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
        {
            return new TextReadResult(automationId, ((ValuePattern)valuePattern).Current.Value ?? string.Empty);
        }

        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
        {
            return new TextReadResult(automationId, ((TogglePattern)togglePattern).Current.ToggleState.ToString());
        }

        return new TextReadResult(automationId, _inspector.ReadElementText(element));
    }

    public ListReadResult ReadList(string automationId, int limit)
    {
        var list = FindControl(automationId);
        return new ListReadResult(automationId, _inspector.ReadListItems(list, limit));
    }

    private AutomationElement GetWindowElement()
    {
        return AutomationElement.FromHandle(_session.RequireWindowHandle());
    }

    private AutomationElement FindControl(string automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            throw new ArgumentException("AutomationId is required.", nameof(automationId));
        }

        var window = GetWindowElement();
        return _inspector.FindControl(window, automationId);
    }

    private static void ClickByBounds(AutomationElement element)
    {
        var rect = element.Current.BoundingRectangle;
        if (rect.IsEmpty)
        {
            throw new InvalidOperationException("Control has no clickable bounds.");
        }

        var centerX = (int)Math.Round(rect.Left + (rect.Width / 2));
        var centerY = (int)Math.Round(rect.Top + (rect.Height / 2));
        NativeMethods.SetCursorPos(centerX, centerY);
        NativeMethods.mouse_event(NativeMethods.MouseEventLeftDown, 0, 0, 0, 0);
        NativeMethods.mouse_event(NativeMethods.MouseEventLeftUp, 0, 0, 0, 0);
    }
}
