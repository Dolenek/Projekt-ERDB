using System;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private string _activeExclusiveBotOperation = string.Empty;

        private bool TryBeginExclusiveBotOperation(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_activeExclusiveBotOperation) &&
                !string.Equals(_activeExclusiveBotOperation, operationName, StringComparison.OrdinalIgnoreCase))
            {
                _log.Info($"[{operationName}] Start ignored while {_activeExclusiveBotOperation} is running.");
                return false;
            }

            _activeExclusiveBotOperation = operationName;
            RefreshBotControlButtonColors();
            return true;
        }

        private void EndExclusiveBotOperation(string operationName)
        {
            if (!string.Equals(_activeExclusiveBotOperation, operationName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _activeExclusiveBotOperation = string.Empty;
            RefreshBotControlButtonColors();
        }

        private bool ShouldBlockForExclusiveBotOperation(string actionName)
        {
            if (string.IsNullOrWhiteSpace(_activeExclusiveBotOperation))
            {
                return false;
            }

            _log.Info($"[{_activeExclusiveBotOperation}] {actionName} ignored while the {_activeExclusiveBotOperation} workflow is running.");
            return true;
        }
    }
}
