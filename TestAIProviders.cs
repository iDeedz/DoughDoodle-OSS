using DoughDoodle.Services;
using DoughDoodle.Models;

namespace DoughDoodle;

public class AIProviderTest
{
    public static async Task TestAllProvidersAsync(IAIProviderManager providerManager)
    {
        try
        {
            Console.WriteLine("=== AI Provider Status ===");
            var providers = providerManager.GetAvailableProviders();
            
            foreach (var provider in providers)
            {
                Console.WriteLine($"Provider: {provider.Name} - Priority: {provider.Priority} - Available: {provider.IsAvailable}");
            }
            
            var activeProvider = providerManager.GetActiveProvider();
            Console.WriteLine($"Active Provider: {activeProvider?.Name ?? "None"}");
            
            // Test basic functionality
            Console.WriteLine("\n=== Testing AI Response ===");
            var testPrompt = "Analyze this budget: Groceries $300 spent of $250 budgeted. What should I do?";
            var response = await providerManager.GenerateResponseAsync(testPrompt);
            Console.WriteLine($"Response: {response.Substring(0, Math.Min(100, response.Length))}...");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing AI providers: {ex.Message}");
        }
    }
}