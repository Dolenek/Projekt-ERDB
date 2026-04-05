using System;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaProviderFactory
    {
        public ICaptchaAnswerProvider Create(CaptchaSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var catalog = CaptchaItemCatalog.Load(settings.ItemNamesFile);
            return new OpenAiCaptchaAnswerProvider(
                new OpenAiChatCompletionApiClient(
                    settings.OpenAiApiKey,
                    TimeSpan.FromSeconds(settings.ApiTimeoutSeconds)),
                catalog,
                settings.OpenAiModel,
                settings.OpenAiRetryModel);
        }
    }
}
