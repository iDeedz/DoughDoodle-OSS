using DoughDoodle.Models;

namespace DoughDoodle.Services;

public interface IAIProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    int Priority { get; }
    bool IsAvailable { get; }
    Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "");
}

public abstract class BaseAIProvider : IAIProvider
{
    protected readonly IConfiguration _configuration;
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;

    protected BaseAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public abstract string Name { get; }
    public abstract bool IsConfigured { get; }
    public abstract int Priority { get; }
    public virtual bool IsAvailable => IsConfigured;

    public abstract Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "");

    protected virtual void LogError(string message, Exception? ex = null)
    {
        _logger.LogError(ex, "[{Provider}] {Message}", Name, message);
    }

    protected virtual void LogInfo(string message)
    {
        _logger.LogInformation("[{Provider}] {Message}", Name, message);
    }
}