using System.Text;
using System.Text.Json;

namespace DoughDoodle.Services;

public class GrokAIProvider : BaseAIProvider
{
    private const string GROK_API_URL = "https://api.x.ai/v1/chat/completions";
    private readonly string? _apiKey;

    public GrokAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GrokAIProvider> logger) 
        : base(httpClient, configuration, logger)
    {
        _apiKey = configuration["GrokApi:ApiKey"];
        if (IsConfigured)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    public override string Name => "GROK";
    public override int Priority => 1; // Highest priority (primary provider)
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
                model = "grok-beta",
                temperature = 0.7,
                max_tokens = 1000
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            LogInfo("Making request to GROK API");
            var response = await _httpClient.PostAsync(GROK_API_URL, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            var result = responseObj.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            LogInfo("Successfully received response from GROK API");
            return result;
        }
        catch (Exception ex)
        {
            LogError("Failed to generate response", ex);
            throw;
        }
    }
}