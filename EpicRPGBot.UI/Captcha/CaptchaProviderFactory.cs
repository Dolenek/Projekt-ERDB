using System;
using EpicRPGBot.UI.Captcha.Local;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaProviderFactory
    {
        public ICaptchaAnswerProvider Create(CaptchaSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new LocalCaptchaAnswerProvider(settings.TemplateDirectory,
                CaptchaItemCatalog.Load(settings.ItemNamesFile), LocalCaptchaPolicy.Load(settings.PolicyFile));
        }
    }
}
