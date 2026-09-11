using System.Threading;

namespace EpicRPGBot.UI.Services
{
    public sealed class InteractivePromptGate
    {
        private int _trainingPending;
        private int _bunnyPending;
        private int _cardHandPending;

        public bool IsAnyPending => IsTrainingPending || IsBunnyPending || IsCardHandPending;
        public bool IsTrainingPending => Interlocked.CompareExchange(ref _trainingPending, 0, 0) == 1;
        public bool IsBunnyPending => Interlocked.CompareExchange(ref _bunnyPending, 0, 0) == 1;
        public bool IsCardHandPending => Interlocked.CompareExchange(ref _cardHandPending, 0, 0) == 1;

        public bool TryBeginTraining()
        {
            return Interlocked.Exchange(ref _trainingPending, 1) == 0;
        }

        public void EndTraining()
        {
            Interlocked.Exchange(ref _trainingPending, 0);
        }

        public bool TryBeginBunny()
        {
            return Interlocked.Exchange(ref _bunnyPending, 1) == 0;
        }

        public void EndBunny()
        {
            Interlocked.Exchange(ref _bunnyPending, 0);
        }

        public bool TryBeginCardHand()
        {
            return Interlocked.Exchange(ref _cardHandPending, 1) == 0;
        }

        public void EndCardHand()
        {
            Interlocked.Exchange(ref _cardHandPending, 0);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _trainingPending, 0);
            Interlocked.Exchange(ref _bunnyPending, 0);
            Interlocked.Exchange(ref _cardHandPending, 0);
        }
    }
}
