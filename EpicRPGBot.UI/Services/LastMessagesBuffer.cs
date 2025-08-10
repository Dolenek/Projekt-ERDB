using System.Collections.ObjectModel;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class LastMessagesBuffer
    {
        public int Capacity { get; }
        public ObservableCollection<MessageItem> Items { get; } = new ObservableCollection<MessageItem>();

        public LastMessagesBuffer(int capacity = 5)
        {
            Capacity = capacity <= 0 ? 5 : capacity;
        }

        public void Add(string text)
        {
            // newest first
            Items.Insert(0, new MessageItem(text));
            // trim to capacity
            while (Items.Count > Capacity)
            {
                Items.RemoveAt(Items.Count - 1);
            }
        }

        public void Clear() => Items.Clear();
    }
}