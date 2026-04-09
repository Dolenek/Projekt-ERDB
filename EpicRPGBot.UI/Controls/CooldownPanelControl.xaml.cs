using System.Windows.Automation;
using System.Windows.Controls;

namespace EpicRPGBot.UI.Controls
{
    public partial class CooldownPanelControl : UserControl
    {
        public CooldownPanelControl()
        {
            InitializeComponent();
        }

        public void ApplyAutomationIds()
        {
            AutomationProperties.SetAutomationId(CooldownsPanel, "CooldownsPanel");
        }
    }
}
