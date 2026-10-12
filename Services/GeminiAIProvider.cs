using System.Text;
using System.Text.Json;

namespace DoughDoodle.Services;

public class GeminiAIProvider : BaseAIProvider
{
    private readonly string? _apiKey;

    public GeminiAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIProvider> logger) 
        : base(httpClient, configuration, logger)
    {
        _apiKey = configuration["Gemini:ApiKey"];
    }

    public override string Name => "Google Gemini";
    public override int Priority => 3; // Third priority
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public override async Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "")
    {
        if (!IsConfigured)
            throw new InvalidOperationException($"{Name} provider is not configured");

        try
        {
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}";
            
            // Combine system prompt with user prompt for Gemini
            var fullPrompt = string.IsNullOrWhiteSpace(systemPrompt) 
                ? $"You are a financial advisor specializing in envelope budgeting and debt management. Provide practical, actionable advice in a structured format.\n\n{prompt}"
                : $"{systemPrompt}\n\n{prompt}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1000
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            LogInfo("Making request to Gemini API");
            var response = await _httpClient.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            var result = responseObj.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            LogInfo("Successfully received response from Gemini API");
            return result;
        }
        catch (Exception ex)
        {
            LogError("Failed to generate response", ex);
            throw;
        }
    }
}