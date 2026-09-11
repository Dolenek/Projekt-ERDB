using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardDeckImageParser
    {
        private const int NormalizedWidth = 600;
        private const int NormalizedHeight = 435;
        private const double OwnedBrightnessThreshold = 145d;
        private const double MinimumThresholdDistance = 7d;

        public CardDeckParseResult Parse(byte[] pngBytes)
        {
            if (pngBytes == null || pngBytes.Length == 0) return Failure("Deck image was empty.");
            try
            {
                using (var source = Cv2.ImDecode(pngBytes, ImreadModes.Color))
                {
                    if (source.Empty()) return Failure("Deck image could not be decoded.");
                    if (!HasExpectedAspectRatio(source)) return Failure("Deck image layout was not recognized.");
                    using (var normalized = source.Resize(new Size(NormalizedWidth, NormalizedHeight)))
                    {
                        if (!HasGreenDeckBackground(normalized)) return Failure("Deck image background was not recognized.");
                        return ReadOwnership(normalized);
                    }
                }
            }
            catch (Exception ex)
            {
                return Failure("Deck image parsing failed: " + ex.Message);
            }
        }

        private static CardDeckParseResult ReadOwnership(Mat image)
        {
            var owned = new List<CardId>();
            var uncertain = new List<CardId>();
            var suits = new[] { CardSuit.Hearts, CardSuit.Diamonds, CardSuit.Clubs, CardSuit.Spades };
            for (var row = 0; row < suits.Length; row++)
            for (var column = 0; column < 13; column++)
            {
                var card = new CardId(suits[row], (CardRank)(column + 2));
                ClassifyCell(image, CreateStandardCell(column, row), card, owned, uncertain);
            }

            ClassifyCell(image, new Rect(544, 300, 36, 57), CardId.Joker, owned, uncertain);
            if (uncertain.Count > 0)
                return Failure("Deck ownership was ambiguous for: " + string.Join(", ", uncertain));
            return new CardDeckParseResult(true, owned, string.Empty);
        }

        private static void ClassifyCell(
            Mat image,
            Rect bounds,
            CardId card,
            ICollection<CardId> owned,
            ICollection<CardId> uncertain)
        {
            using (var cell = new Mat(image, bounds))
            using (var gray = new Mat())
            {
                Cv2.CvtColor(cell, gray, ColorConversionCodes.BGR2GRAY);
                var brightness = Cv2.Mean(gray).Val0;
                if (Math.Abs(brightness - OwnedBrightnessThreshold) < MinimumThresholdDistance)
                {
                    uncertain.Add(card);
                    return;
                }

                if (brightness >= OwnedBrightnessThreshold || LooksGoldened(cell)) owned.Add(card);
            }
        }

        private static bool LooksGoldened(Mat cell)
        {
            using (var hsv = new Mat())
            {
                Cv2.CvtColor(cell, hsv, ColorConversionCodes.BGR2HSV);
                var mean = Cv2.Mean(hsv);
                return mean.Val0 >= 10d && mean.Val0 <= 40d && mean.Val1 >= 75d && mean.Val2 >= 105d;
            }
        }

        private static Rect CreateStandardCell(int column, int row)
        {
            return new Rect(20 + (int)Math.Round(column * 43.67d), 18 + (row * 70), 36, 63);
        }

        private static bool HasExpectedAspectRatio(Mat image)
        {
            var ratio = (double)image.Width / image.Height;
            return ratio >= 1.25d && ratio <= 1.55d;
        }

        private static bool HasGreenDeckBackground(Mat image)
        {
            var greenPixels = 0;
            var samplePixels = 0;
            for (var y = 5; y < image.Height; y += 10)
            for (var x = 5; x < image.Width; x += 10)
            {
                var pixel = image.At<Vec3b>(y, x);
                if (pixel.Item1 > pixel.Item0 * 1.25 && pixel.Item1 > pixel.Item2 * 1.25) greenPixels++;
                samplePixels++;
            }

            return samplePixels > 0 && (double)greenPixels / samplePixels >= 0.20d;
        }

        private static CardDeckParseResult Failure(string error)
        {
            return new CardDeckParseResult(false, null, error);
        }
    }
}
