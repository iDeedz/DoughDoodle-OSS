using DoughDoodle.Models;

namespace DoughDoodle.Services;

public interface IAIProviderManager
{
    Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "");
    List<IAIProvider> GetAvailableProviders();
    IAIProvider? GetActiveProvider();
}

public class AIProviderManager : IAIProviderManager
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly ILogger<AIProviderManager> _logger;

    public AIProviderManager(IEnumerable<IAIProvider> providers, ILogger<AIProviderManager> logger)
    {
        _providers = providers.OrderBy(p => p.Priority);
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "")
    {
        var availableProviders = GetAvailableProviders();
        
        foreach (var provider in availableProviders)
        {
            try
            {
                _logger.LogInformation("Attempting to use AI provider: {Provider}", provider.Name);
                var result = await provider.GenerateResponseAsync(prompt, systemPrompt);
                
                if (!string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogInformation("Successfully generated response using provider: {Provider}", provider.Name);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed, trying next provider", provider.Name);
                continue;
            }
        }

        _logger.LogError("All AI providers failed");
        throw new InvalidOperationException("All AI providers failed to generate a response");
    }

    public List<IAIProvider> GetAvailableProviders()
    {
        return _providers
            .Where(p => p.IsConfigured && p.IsAvailable)
            .OrderBy(p => p.Priority)
            .ToList();
    }

    public IAIProvider? GetActiveProvider()
    {
        return GetAvailableProviders().FirstOrDefault();
    }
}