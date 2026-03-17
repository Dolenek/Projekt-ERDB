using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace EpicRPGBot.UI.Crafting
{
    public partial class CraftingWindow : Window
    {
        private readonly Func<CraftRequest, Action<string>, CancellationToken, Task<CraftJobResult>> _runCraftAsync;
        private CancellationTokenSource _craftingCancellation;
        private bool _closeWhenIdle;
        private bool _isCrafting;

        public CraftingWindow(Func<CraftRequest, Action<string>, CancellationToken, Task<CraftJobResult>> runCraftAsync)
        {
            _runCraftAsync = runCraftAsync ?? throw new ArgumentNullException(nameof(runCraftAsync));
            InitializeComponent();
            ApplyAutomationSurface();
            SetDefaultAmounts();
        }

        private async void CraftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isCrafting)
            {
                return;
            }

            if (!TryBuildRequest(out var request))
            {
                return;
            }

            _craftingCancellation = new CancellationTokenSource();
            SetCraftingState(true);
            AppendStatusLine("Crafting started.");

            try
            {
                var result = await _runCraftAsync(request, AppendStatusLine, _craftingCancellation.Token);
                AppendStatusLine(result.Summary);
            }
            catch (Exception ex)
            {
                AppendStatusLine("Crafting failed: " + ex.Message);
            }
            finally
            {
                _craftingCancellation.Dispose();
                _craftingCancellation = null;
                SetCraftingState(false);
                if (_closeWhenIdle)
                {
                    Close();
                }
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCrafting)
            {
                Close();
                return;
            }

            if (_closeWhenIdle)
            {
                return;
            }

            _closeWhenIdle = true;
            AppendStatusLine("Cancellation requested. Waiting for the current command to finish.");
            _craftingCancellation?.Cancel();
            CancelBtn.Content = "Cancelling...";
        }

        private bool TryBuildRequest(out CraftRequest request)
        {
            request = null;

            if (!TryReadAmount(EpicAmountBox, "epic log", out var epicAmount) ||
                !TryReadAmount(SuperAmountBox, "super log", out var superAmount) ||
                !TryReadAmount(MegaAmountBox, "mega log", out var megaAmount) ||
                !TryReadAmount(HyperAmountBox, "hyper log", out var hyperAmount) ||
                !TryReadAmount(UltraAmountBox, "ultra log", out var ultraAmount) ||
                !TryReadAmount(EpicFishAmountBox, "epic fish", out var epicFishAmount) ||
                !TryReadAmount(GoldenFishAmountBox, "golden fish", out var goldenFishAmount) ||
                !TryReadAmount(BananaAmountBox, "banana", out var bananaAmount))
            {
                return false;
            }

            request = new CraftRequest(new Dictionary<CraftItemKey, long>
            {
                { CraftItemKey.EpicLog, epicAmount },
                { CraftItemKey.SuperLog, superAmount },
                { CraftItemKey.MegaLog, megaAmount },
                { CraftItemKey.HyperLog, hyperAmount },
                { CraftItemKey.UltraLog, ultraAmount },
                { CraftItemKey.EpicFish, epicFishAmount },
                { CraftItemKey.GoldenFish, goldenFishAmount },
                { CraftItemKey.Banana, bananaAmount }
            });

            if (request.HasAnyAmount)
            {
                return true;
            }

            AppendStatusLine("Enter at least one craft amount.");
            return false;
        }

        private bool TryReadAmount(TextBox textBox, string itemName, out long amount)
        {
            var rawValue = textBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                amount = 0;
                return true;
            }

            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount) && amount >= 0)
            {
                return true;
            }

            AppendStatusLine($"Invalid amount for {itemName}: {rawValue}");
            return false;
        }

        private void AppendStatusLine(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var line = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    return;
                }

                StatusBox.AppendText(line + Environment.NewLine);
                StatusBox.ScrollToEnd();
            });
        }

        private void SetCraftingState(bool isCrafting)
        {
            _isCrafting = isCrafting;
            CraftBtn.IsEnabled = !isCrafting;
            EpicAmountBox.IsEnabled = !isCrafting;
            SuperAmountBox.IsEnabled = !isCrafting;
            MegaAmountBox.IsEnabled = !isCrafting;
            HyperAmountBox.IsEnabled = !isCrafting;
            UltraAmountBox.IsEnabled = !isCrafting;
            EpicFishAmountBox.IsEnabled = !isCrafting;
            GoldenFishAmountBox.IsEnabled = !isCrafting;
            BananaAmountBox.IsEnabled = !isCrafting;
            CancelBtn.Content = isCrafting ? "Cancel" : "Close";
        }

        private void SetDefaultAmounts()
        {
            EpicAmountBox.Text = "0";
            SuperAmountBox.Text = "0";
            MegaAmountBox.Text = "0";
            HyperAmountBox.Text = "0";
            UltraAmountBox.Text = "0";
            EpicFishAmountBox.Text = "0";
            GoldenFishAmountBox.Text = "0";
            BananaAmountBox.Text = "0";
        }

        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "CraftingWindow");
            SetAutomationIdentity(EpicAmountBox, "CraftEpicAmountInput");
            SetAutomationIdentity(SuperAmountBox, "CraftSuperAmountInput");
            SetAutomationIdentity(MegaAmountBox, "CraftMegaAmountInput");
            SetAutomationIdentity(HyperAmountBox, "CraftHyperAmountInput");
            SetAutomationIdentity(UltraAmountBox, "CraftUltraAmountInput");
            SetAutomationIdentity(EpicFishAmountBox, "CraftEpicFishAmountInput");
            SetAutomationIdentity(GoldenFishAmountBox, "CraftGoldenFishAmountInput");
            SetAutomationIdentity(BananaAmountBox, "CraftBananaAmountInput");
            SetAutomationIdentity(StatusBox, "CraftStatusText");
            SetAutomationIdentity(CraftBtn, "CraftStartButton");
            SetAutomationIdentity(CancelBtn, "CraftCancelButton");
        }

        private static void SetAutomationIdentity(DependencyObject element, string automationId)
        {
            if (element == null || string.IsNullOrWhiteSpace(automationId))
            {
                return;
            }

            AutomationProperties.SetAutomationId(element, automationId);
        }
    }
}
