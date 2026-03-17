using EpicRPGBot.Mcp.Models;
using System.Windows.Automation;

namespace EpicRPGBot.Mcp.Services;

public sealed partial class UiAutomationFacade
{
    public async Task<ListReadResult> ReadSidebarListAsync(string tabAutomationId, string listAutomationId, int limit, int timeoutMs = 1200)
    {
        try
        {
            var status = _session.GetStatus();
            if (!status.IsRunning)
            {
                return new ListReadResult(listAutomationId, Array.Empty<string>(), 0, tabAutomationId, false, false, "EpicRPGBot.UI is not running.", status);
            }

            _session.BringToFront();
            TrySelectTab(tabAutomationId);

            var timeout = TimeSpan.FromMilliseconds(Math.Max(200, timeoutMs));
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow <= deadline)
            {
                var list = TryFindControl(listAutomationId);
                if (list != null)
                {
                    var items = _inspector.ReadListItems(list, limit);
                    return new ListReadResult(listAutomationId, items, items.Count, tabAutomationId, true, true, string.Empty, status);
                }

                await Task.Delay(100);
            }

            return new ListReadResult(listAutomationId, Array.Empty<string>(), 0, tabAutomationId, false, false, $"Could not find list '{listAutomationId}' after selecting '{tabAutomationId}'.", status);
        }
        catch (Exception ex)
        {
            return new ListReadResult(listAutomationId, Array.Empty<string>(), 0, tabAutomationId, false, false, ex.Message, _session.GetStatus());
        }
    }

    public async Task<WaitForTextResult> WaitForControlTextAsync(string automationId, string textContains, int timeoutMs = 5000)
    {
        try
        {
            var status = _session.GetStatus();
            if (!status.IsRunning)
            {
                return new WaitForTextResult(automationId, string.Empty, false, false, "EpicRPGBot.UI is not running.", status);
            }

            if (string.IsNullOrWhiteSpace(textContains))
            {
                return new WaitForTextResult(automationId, string.Empty, false, false, "textContains is required.", status);
            }

            _session.BringToFront();

            var timeout = TimeSpan.FromMilliseconds(Math.Max(200, timeoutMs));
            var deadline = DateTime.UtcNow + timeout;
            var lastValue = string.Empty;

            while (DateTime.UtcNow <= deadline)
            {
                var control = TryFindControl(automationId);
                if (control != null)
                {
                    lastValue = _inspector.ReadElementText(control);
                    if (lastValue.IndexOf(textContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new WaitForTextResult(automationId, lastValue, true, true, string.Empty, status);
                    }
                }

                await Task.Delay(100);
            }

            return new WaitForTextResult(automationId, lastValue, false, true, string.Empty, status);
        }
        catch (Exception ex)
        {
            return new WaitForTextResult(automationId, string.Empty, false, false, ex.Message, _session.GetStatus());
        }
    }

    private void TrySelectTab(string tabAutomationId)
    {
        var tab = TryFindControl(tabAutomationId);
        if (tab == null)
        {
            return;
        }

        if (tab.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            ((InvokePattern)invokePattern).Invoke();
            return;
        }

        if (tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
        {
            ((SelectionItemPattern)selectionPattern).Select();
            return;
        }

        ClickByBounds(tab);
    }

    private AutomationElement? TryFindControl(string automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            return null;
        }

        return _inspector.TryFindControl(GetWindowElement(), automationId);
    }
}
