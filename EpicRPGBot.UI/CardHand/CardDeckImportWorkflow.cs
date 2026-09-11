using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardDeckImportWorkflow
    {
        private readonly ConfirmedCommandSender _commandSender;
        private readonly DiscordAttachmentImageSource _imageSource;
        private readonly CardDeckImageParser _imageParser;

        public CardDeckImportWorkflow(
            IDiscordChatClient chatClient,
            CardDeckImageParser imageParser = null)
        {
            if (chatClient == null) throw new ArgumentNullException(nameof(chatClient));
            _commandSender = new ConfirmedCommandSender(chatClient);
            _imageSource = new DiscordAttachmentImageSource(chatClient);
            _imageParser = imageParser ?? new CardDeckImageParser();
        }

        public async Task<CardDeckImportResult> RunAsync(
            Action onOutgoingRegistered,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = await _commandSender.SendAsync(
                    "rpg card deck",
                    snapshot => onOutgoingRegistered?.Invoke(),
                    cancellationToken);
                if (!command.IsConfirmed)
                    return Failure("EPIC RPG did not confirm the card deck command.");

                var bytes = await _imageSource.LoadAsync(command.ReplyMessage.Id, cancellationToken);
                if (bytes == null) return Failure("The card deck attachment was not found.");
                var parsed = _imageParser.Parse(bytes);
                return parsed.Success
                    ? new CardDeckImportResult(true, parsed.OwnedCards, $"Loaded {parsed.OwnedCards.Count}/53 owned cards.")
                    : Failure(parsed.Error);
            }
            catch (OperationCanceledException)
            {
                return Failure("Card deck loading was cancelled.");
            }
            catch (Exception ex)
            {
                return Failure("Card deck loading failed: " + ex.Message);
            }
        }

        private static CardDeckImportResult Failure(string message)
        {
            return new CardDeckImportResult(false, null, message);
        }
    }
}
