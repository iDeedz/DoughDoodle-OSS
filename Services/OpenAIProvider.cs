using System.Text;
using System.Text.Json;

namespace DoughDoodle.Services;

public class OpenAIProvider : BaseAIProvider
{
    private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
    private readonly string? _apiKey;

    public OpenAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIProvider> logger) 
        : base(httpClient, configuration, logger)
    {
        _apiKey = configuration["OpenAI:ApiKey"];
        if (IsConfigured)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    public override string Name => "OpenAI";
    public override int Priority => 2; // Secondary provider
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public override async Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "")
    {
        if (!IsConfigured)
            throw new InvalidOperationException($"{Name} provider is not configured");

        try
        {
            var messages = new List<object>();
            
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }
            else
            {
                messages.Add(new { role = "system", content = "You are a financial advisor specializing in envelope budgeting and debt management. Provide practical, actionable advice in a structured format." });
            }
            
            messages.Add(new { role = "user", content = prompt });

            var requestBody = new
            {
                messages = messages.ToArray(),
                model = "gpt-3.5-turbo", // Using the free tier model
                temperature = 0.7,
                max_tokens = 1000
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            LogInfo("Making request to OpenAI API");
            var response = await _httpClient.PostAsync(OPENAI_API_URL, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            var result = responseObj.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            LogInfo("Successfully received response from OpenAI API");
            return result;
        }
        catch (Exception ex)
        {
            LogError("Failed to generate response", ex);
            throw;
        }
    }
}