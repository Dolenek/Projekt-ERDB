using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class OpenAiChatCompletionsRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OpenAiChatMessage> Messages { get; set; }

        [JsonPropertyName("response_format")]
        public OpenAiResponseFormat ResponseFormat { get; set; }
    }

    public sealed class OpenAiChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public List<OpenAiChatMessageContentPart> Content { get; set; }
    }

    public sealed class OpenAiChatMessageContentPart
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Text { get; set; }

        [JsonPropertyName("image_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenAiImageUrl ImageUrl { get; set; }
    }

    public sealed class OpenAiImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }
    }

    public sealed class OpenAiResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("json_schema")]
        public OpenAiJsonSchema JsonSchema { get; set; }
    }

    public sealed class OpenAiJsonSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("strict")]
        public bool Strict { get; set; }

        [JsonPropertyName("schema")]
        public object Schema { get; set; }
    }

    public sealed class OpenAiChatCompletionsResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChatChoice> Choices { get; set; }
    }

    public sealed class OpenAiChatChoice
    {
        [JsonPropertyName("message")]
        public OpenAiChatResponseMessage Message { get; set; }
    }

    public sealed class OpenAiChatResponseMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("refusal")]
        public string Refusal { get; set; }
    }

    public sealed class OpenAiErrorEnvelope
    {
        [JsonPropertyName("error")]
        public OpenAiErrorBody Error { get; set; }
    }

    public sealed class OpenAiErrorBody
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public sealed class CaptchaSelectionPayload
    {
        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("item_index")]
        public int ItemIndex { get; set; }
    }
}
