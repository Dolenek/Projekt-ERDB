namespace EpicRPGBot.UI.Automation
{
    public static class AutomationRuntime
    {
        public static AutomationOptions Current { get; private set; } = new AutomationOptions();

        public static void Initialize(AutomationOptions options)
        {
            Current = options ?? new AutomationOptions();
        }
    }
}
