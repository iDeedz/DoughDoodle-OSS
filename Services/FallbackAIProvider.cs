namespace DoughDoodle.Services;

public class FallbackAIProvider : BaseAIProvider
{
    public FallbackAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger<FallbackAIProvider> logger) 
        : base(httpClient, configuration, logger)
    {
    }

    public override string Name => "Fallback";
    public override int Priority => 999; // Lowest priority (last resort)
    public override bool IsConfigured => true; // Always available

    public override Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "")
    {
        LogInfo("Using fallback AI provider - returning basic advice");
        
        // Return basic financial advice when all other providers fail
        var fallbackResponse = GenerateBasicFinancialAdvice(prompt);
        return Task.FromResult(fallbackResponse);
    }

    private string GenerateBasicFinancialAdvice(string prompt)
    {
        // Simple pattern matching for basic advice
        var lowerPrompt = prompt.ToLower();
        
        if (lowerPrompt.Contains("budget"))
        {
            return @"Main Advice: Focus on tracking your expenses and staying within your planned budget amounts.

Tips:
• Review your spending weekly to stay on track
• Consider the 50/30/20 rule: 50% needs, 30% wants, 20% savings
• Set up automatic transfers to savings accounts
• Use cash envelopes for discretionary spending
• Look for areas where you can reduce unnecessary expenses

Overall Health Score: Good - Keep monitoring your progress";
        }
        
        if (lowerPrompt.Contains("debt"))
        {
            return @"Debt Payoff Strategy: Consider using the debt avalanche method (pay minimums on all debts, put extra money toward highest interest rate debt).

Key Steps:
• List all debts with balances, minimum payments, and interest rates
• Make minimum payments on all debts
• Put any extra money toward the debt with the highest interest rate
• Consider debt consolidation if it lowers your overall interest rate

Estimated Timeline: Varies based on your extra payment amount and debt balances.";
        }
        
        if (lowerPrompt.Contains("savings") || lowerPrompt.Contains("save"))
        {
            return @"Savings Optimization: Build a solid emergency fund and automate your savings.

Cost-Cutting Tips:
• Review and cancel unused subscriptions
• Plan meals to reduce food waste
• Compare insurance rates annually
• Use generic brands for household items
• Look for free or low-cost entertainment options

Goal: Aim to save 20% of your income, starting with a $1000 emergency fund.";
        }
        
        return @"General Financial Advice: Focus on the basics of personal finance.

Key Principles:
• Track all income and expenses
• Create and stick to a monthly budget
• Build an emergency fund (3-6 months of expenses)
• Pay off high-interest debt first
• Save consistently, even if it's a small amount
• Review and adjust your financial plan regularly

Remember: Small, consistent steps lead to big financial improvements over time.";
    }
}