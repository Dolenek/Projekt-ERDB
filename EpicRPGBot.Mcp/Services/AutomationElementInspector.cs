using System.Windows.Automation;

namespace EpicRPGBot.Mcp.Services;

public sealed class AutomationElementInspector
{
    public AutomationElement FindControl(AutomationElement window, string automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            throw new ArgumentException("AutomationId is required.", nameof(automationId));
        }

        var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
        return window.FindFirst(TreeScope.Descendants, condition)
            ?? throw new InvalidOperationException($"Could not find control '{automationId}'.");
    }

    public IReadOnlyList<string> ReadListItems(AutomationElement list, int limit)
    {
        var items = list.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
        var values = new List<string>();

        foreach (AutomationElement item in items)
        {
            var text = ReadElementText(item);
            if (!string.IsNullOrWhiteSpace(text))
            {
                values.Add(text);
            }
        }

        return values.TakeLast(Math.Max(1, limit)).ToArray();
    }

    public string ReadElementText(AutomationElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.Current.Name))
        {
            return element.Current.Name;
        }

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
        {
            return ((ValuePattern)valuePattern).Current.Value ?? string.Empty;
        }

        var textNodes = element.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));
        var values = new List<string>();
        foreach (AutomationElement node in textNodes)
        {
            if (!string.IsNullOrWhiteSpace(node.Current.Name))
            {
                values.Add(node.Current.Name);
            }
        }

        return string.Join(Environment.NewLine, values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
    }

    public string FriendlyControlType(ControlType controlType)
    {
        var programmaticName = controlType?.ProgrammaticName ?? "ControlType.Custom";
        var index = programmaticName.LastIndexOf('.') + 1;
        return index > 0 && index < programmaticName.Length
            ? programmaticName.Substring(index)
            : programmaticName;
    }
}
