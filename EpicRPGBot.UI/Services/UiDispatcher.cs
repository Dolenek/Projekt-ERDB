using System;
using System.Windows;
using System.Windows.Threading;

namespace EpicRPGBot.UI.Services
{
    public static class UiDispatcher
    {
        public static Dispatcher Dispatcher => Application.Current?.Dispatcher;

        public static void OnUI(Action action)
        {
            if (Dispatcher == null)
            {
                action?.Invoke();
                return;
            }

            if (Dispatcher.CheckAccess())
                action?.Invoke();
            else
                Dispatcher.BeginInvoke(action);
        }

        public static void OnUI<T>(Action<T> action, T arg)
        {
            if (Dispatcher == null)
            {
                action?.Invoke(arg);
                return;
            }

            if (Dispatcher.CheckAccess())
                action?.Invoke(arg);
            else
                Dispatcher.BeginInvoke(action, arg);
        }
    }
}