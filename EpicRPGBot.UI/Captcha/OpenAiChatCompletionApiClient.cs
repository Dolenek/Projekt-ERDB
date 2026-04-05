using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class OpenAiChatCompletionApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        private readonly HttpClient _httpClient;

        public OpenAiChatCompletionApiClient(string apiKey, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("OpenAI API key is required.", nameof(apiKey));
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.openai.com/"),
                Timeout = timeout
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        }

        public async Task<OpenAiChatCompletionsResponse> CreateCompletionAsync(OpenAiChatCompletionsRequest request, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using (var message = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions"))
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                message.Content = content;
                using (var response = await _httpClient.SendAsync(message, cancellationToken))
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(BuildErrorMessage((int)response.StatusCode, body));
                    }

                    var parsed = JsonSerializer.Deserialize<OpenAiChatCompletionsResponse>(body, JsonOptions);
                    if (parsed == null)
                    {
                        throw new InvalidOperationException("OpenAI returned an empty completion response.");
                    }

                    return parsed;
                }
            }
        }

        private static string BuildErrorMessage(int statusCode, string responseBody)
        {
            try
            {
                var error = JsonSerializer.Deserialize<OpenAiErrorEnvelope>(responseBody, JsonOptions);
                if (error?.Error != null && !string.IsNullOrWhiteSpace(error.Error.Message))
                {
                    return $"OpenAI API error {statusCode}: {error.Error.Message}";
                }
            }
            catch
            {
            }

            return $"OpenAI API error {statusCode}.";
        }
    }
}
