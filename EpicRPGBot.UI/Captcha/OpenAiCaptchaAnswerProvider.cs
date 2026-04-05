using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class OpenAiCaptchaAnswerProvider : ICaptchaAnswerProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly OpenAiChatCompletionApiClient _apiClient;
        private readonly CaptchaItemCatalog _catalog;
        private readonly string _primaryModel;
        private readonly string _retryModel;

        public OpenAiCaptchaAnswerProvider(
            OpenAiChatCompletionApiClient apiClient,
            CaptchaItemCatalog catalog,
            string primaryModel,
            string retryModel)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _primaryModel = string.IsNullOrWhiteSpace(primaryModel) ? "gpt-5-mini" : primaryModel.Trim();
            _retryModel = string.IsNullOrWhiteSpace(retryModel) ? "gpt-5" : retryModel.Trim();
        }

        public string DescribeConfiguration()
        {
            return $"mode=openai, model={_primaryModel}, retryModel={_retryModel}, catalogItems={_catalog.Count}";
        }

        public async Task<CaptchaAnswerResult> SolveAsync(byte[] imageBytes, CancellationToken cancellationToken)
        {
            var primaryResult = await TrySolveWithModelAsync(_primaryModel, imageBytes, false, cancellationToken);
            if (primaryResult.IsMatch)
            {
                return primaryResult;
            }

            var retryImage = CaptchaImagePreprocessor.CreateRetryImage(imageBytes) ?? imageBytes;
            var retryResult = await TrySolveWithModelAsync(_retryModel, retryImage, true, cancellationToken);
            if (retryResult.IsMatch)
            {
                return retryResult;
            }

            return CaptchaAnswerResult.NoMatch(
                _retryModel,
                $"primary={primaryResult.Detail}; retry={retryResult.Detail}");
        }

        private async Task<CaptchaAnswerResult> TrySolveWithModelAsync(
            string model,
            byte[] imageBytes,
            bool enhancedRetryImage,
            CancellationToken cancellationToken)
        {
            var request = BuildRequest(model, imageBytes, enhancedRetryImage);
            var response = await _apiClient.CreateCompletionAsync(request, cancellationToken);
            var message = response?.Choices != null && response.Choices.Count > 0
                ? response.Choices[0].Message
                : null;

            if (message == null)
            {
                return CaptchaAnswerResult.NoMatch(model, "empty_model_response");
            }

            if (!string.IsNullOrWhiteSpace(message.Refusal))
            {
                return CaptchaAnswerResult.NoMatch(model, "model_refusal");
            }

            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return CaptchaAnswerResult.NoMatch(model, "empty_message_content");
            }

            var payload = JsonSerializer.Deserialize<CaptchaSelectionPayload>(message.Content, JsonOptions);
            if (payload == null)
            {
                return CaptchaAnswerResult.NoMatch(model, "invalid_json_payload");
            }

            var result = (payload.Result ?? string.Empty).Trim().ToLowerInvariant();
            if (result == "unknown")
            {
                return CaptchaAnswerResult.NoMatch(model, $"unknown_response, enhanced={enhancedRetryImage}");
            }

            var label = _catalog.GetItemName(payload.ItemIndex);
            if (!string.Equals(result, "match", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(label))
            {
                return CaptchaAnswerResult.NoMatch(
                    model,
                    $"invalid_selection(result={payload.Result}, item_index={payload.ItemIndex}, enhanced={enhancedRetryImage})");
            }

            return CaptchaAnswerResult.Match(
                label,
                model,
                $"item_index={payload.ItemIndex}, enhanced={enhancedRetryImage}");
        }

        private OpenAiChatCompletionsRequest BuildRequest(string model, byte[] imageBytes, bool enhancedRetryImage)
        {
            var mediaType = CaptchaImageMediaTypeDetector.Detect(imageBytes);
            var imageUrl = $"data:{mediaType};base64," + Convert.ToBase64String(imageBytes ?? System.Array.Empty<byte>());

            return new OpenAiChatCompletionsRequest
            {
                Model = model,
                Messages = new List<OpenAiChatMessage>
                {
                    new OpenAiChatMessage
                    {
                        Role = "system",
                        Content = new List<OpenAiChatMessageContentPart>
                        {
                            new OpenAiChatMessageContentPart
                            {
                                Type = "text",
                                Text = CaptchaVisionPromptBuilder.BuildSystemPrompt()
                            }
                        }
                    },
                    new OpenAiChatMessage
                    {
                        Role = "user",
                        Content = new List<OpenAiChatMessageContentPart>
                        {
                            new OpenAiChatMessageContentPart
                            {
                                Type = "text",
                                Text = CaptchaVisionPromptBuilder.BuildUserPrompt(_catalog, enhancedRetryImage)
                            },
                            new OpenAiChatMessageContentPart
                            {
                                Type = "image_url",
                                ImageUrl = new OpenAiImageUrl
                                {
                                    Url = imageUrl,
                                    Detail = "high"
                                }
                            }
                        }
                    }
                },
                ResponseFormat = new OpenAiResponseFormat
                {
                    Type = "json_schema",
                    JsonSchema = new OpenAiJsonSchema
                    {
                        Name = "captcha_selection",
                        Strict = true,
                        Schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                result = new
                                {
                                    type = "string",
                                    @enum = new[] { "match", "unknown" }
                                },
                                item_index = new
                                {
                                    type = "integer",
                                    minimum = 0,
                                    maximum = _catalog.Count
                                }
                            },
                            required = new[] { "result", "item_index" }
                        }
                    }
                }
            };
        }
    }
}
