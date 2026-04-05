namespace EpicRPGBot.UI.Captcha
{
    public static class CaptchaImageMediaTypeDetector
    {
        public static string Detect(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 12)
            {
                return "image/png";
            }

            if (HasPrefix(imageBytes, 0x89, 0x50, 0x4E, 0x47))
            {
                return "image/png";
            }

            if (HasPrefix(imageBytes, 0xFF, 0xD8, 0xFF))
            {
                return "image/jpeg";
            }

            if (HasPrefix(imageBytes, 0x47, 0x49, 0x46, 0x38))
            {
                return "image/gif";
            }

            if (HasPrefix(imageBytes, 0x52, 0x49, 0x46, 0x46) &&
                imageBytes[8] == 0x57 &&
                imageBytes[9] == 0x45 &&
                imageBytes[10] == 0x42 &&
                imageBytes[11] == 0x50)
            {
                return "image/webp";
            }

            return "image/png";
        }

        private static bool HasPrefix(byte[] imageBytes, params byte[] prefix)
        {
            if (imageBytes.Length < prefix.Length)
            {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++)
            {
                if (imageBytes[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
