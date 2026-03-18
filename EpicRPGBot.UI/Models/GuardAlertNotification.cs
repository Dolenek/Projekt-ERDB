namespace EpicRPGBot.UI.Models
{
    public enum GuardAlertKind
    {
        FirstDetected,
        Reminder,
        Cleared
    }

    public sealed class GuardAlertNotification
    {
        public GuardAlertNotification(GuardAlertKind kind, string message)
        {
            Kind = kind;
            Message = message ?? string.Empty;
        }

        public GuardAlertKind Kind { get; }
        public string Message { get; }
        public bool ShouldShowBalloon => Kind != GuardAlertKind.Cleared;
        public bool ShouldPlaySound => Kind == GuardAlertKind.FirstDetected;
        public bool ShouldBringToFront => Kind == GuardAlertKind.FirstDetected;
    }
}
