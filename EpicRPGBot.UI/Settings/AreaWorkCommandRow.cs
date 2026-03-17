using System;
using System.ComponentModel;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Settings
{
    public sealed class AreaWorkCommandRow : INotifyPropertyChanged
    {
        private string _commandText;

        public AreaWorkCommandRow(int area, string commandText)
        {
            Area = area;
            _commandText = commandText ?? string.Empty;
        }

        public int Area { get; }

        public string CommandText
        {
            get => _commandText;
            set
            {
                var nextValue = value ?? string.Empty;
                if (string.Equals(_commandText, nextValue, StringComparison.Ordinal))
                {
                    return;
                }

                _commandText = nextValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommandText)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
