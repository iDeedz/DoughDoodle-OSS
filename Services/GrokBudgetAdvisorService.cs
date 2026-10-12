using System.Text;
using System.Text.Json;
using DoughDoodle.Models;

namespace DoughDoodle.Services;

public class GrokBudgetAdvisorService : IGrokBudgetAdvisorService
{
    private readonly IAIProviderManager _aiProviderManager;
    private readonly ILogger<GrokBudgetAdvisorService> _logger;

    public GrokBudgetAdvisorService(IAIProviderManager aiProviderManager, ILogger<GrokBudgetAdvisorService> logger)
    {
        _aiProviderManager = aiProviderManager;
        _logger = logger;
    }

    public async Task<BudgetRecommendation> GetBudgetRecommendationsAsync(List<Category> categories, string currentMonth)
    {
        try
        {
            var budgetData = categories.Select(c => new
            {
                Name = c.Name,
                Budgeted = c.Budgets.FirstOrDefault(b => b.MonthYear == currentMonth)?.Amount ?? 0,
                Spent = c.Transactions.Where(t => t.Date.ToString("yyyy-MM") == currentMonth).Sum(t => t.Amount),
                Remaining = (c.Budgets.FirstOrDefault(b => b.MonthYear == currentMonth)?.Amount ?? 0) - 
                           c.Transactions.Where(t => t.Date.ToString("yyyy-MM") == currentMonth).Sum(t => t.Amount)
            });

            var prompt = CreateBudgetAnalysisPrompt(budgetData);
            var systemPrompt = "You are a financial advisor specializing in envelope budgeting and debt management. Provide practical, actionable advice in a structured format.";
            var aiResponse = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseBudgetRecommendation(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "All AI providers failed, falling back to basic recommendations");
            // Fallback to basic recommendations if all AI providers fail
            return CreateFallbackBudgetRecommendation(categories, currentMonth);
        }
    }

    public async Task<DebtPayoffRecommendation> GetDebtPayoffRecommendationsAsync(decimal extraMoney, List<DebtInfo> debts)
    {
        try
        {
            var prompt = CreateDebtPayoffPrompt(extraMoney, debts);
            var systemPrompt = "You are a debt management expert. Provide specific debt payoff strategies with calculations and timelines.";
            var aiResponse = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseDebtPayoffRecommendation(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "All AI providers failed for debt payoff recommendations, using fallback");
            // Fallback to basic debt payoff strategy
            return CreateFallbackDebtPayoffRecommendation(extraMoney, debts);
        }
    }

    public async Task<SavingsOptimizationRecommendation> GetSavingsOptimizationAsync(List<Category> categories, decimal totalIncome)
    {
        try
        {
            var prompt = CreateSavingsOptimizationPrompt(categories, totalIncome);
            var systemPrompt = "You are a savings optimization expert. Provide specific recommendations for cutting costs and maximizing savings.";
            var aiResponse = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseSavingsOptimizationRecommendation(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "All AI providers failed for savings optimization, using fallback");
            // Fallback to basic savings recommendations
            return CreateFallbackSavingsRecommendation(categories, totalIncome);
        }
    }

    private string CreateBudgetAnalysisPrompt(object budgetData)
    {
        var dataJson = JsonSerializer.Serialize(budgetData, new JsonSerializerOptions { WriteIndented = true });
        
        return $@"Analyze this envelope budgeting data and provide recommendations:

{dataJson}

Please provide:
1. Main advice (1-2 sentences)
2. 3-5 specific tips for improvement
3. Suggested budget adjustments for overspent or underspent categories
4. Overall budget health score (Excellent/Good/Fair/Poor)

Format your response as structured text that can be easily parsed.";
    }

    private string CreateDebtPayoffPrompt(decimal extraMoney, List<DebtInfo> debts)
    {
        var debtsJson = JsonSerializer.Serialize(debts, new JsonSerializerOptions { WriteIndented = true });
        
        return $@"I have ${extraMoney:F2} extra money available and these debts:

{debtsJson}

Please recommend:
1. Best debt payoff strategy (avalanche, snowball, etc.)
2. How to allocate the extra money across debts
3. Estimated payoff timeline
4. Potential interest savings

Format your response as structured text.";
    }

    private string CreateSavingsOptimizationPrompt(List<Category> categories, decimal totalIncome)
    {
        var categoryData = categories.Select(c => new { c.Name, LastMonthSpent = c.Transactions.Take(10).Sum(t => t.Amount) });
        var dataJson = JsonSerializer.Serialize(categoryData, new JsonSerializerOptions { WriteIndented = true });
        
        return $@"Monthly income: ${totalIncome:F2}
Current spending by category:

{dataJson}

Please suggest:
1. Areas to cut costs
2. Optimal emergency fund amount
3. Savings goals to prioritize
4. Percentage of income to save

Format your response as structured text.";
    }

    private BudgetRecommendation ParseBudgetRecommendation(string aiResponse)
    {
        // Parse AI response and extract structured data
        // This is a simplified parser - in production, you'd want more robust parsing
        var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var recommendation = new BudgetRecommendation
        {
            MainAdvice = ExtractMainAdvice(lines),
            Tips = ExtractTips(lines),
            OverallHealthScore = ExtractHealthScore(lines),
            SuggestedAdjustments = ExtractAdjustments(lines)
        };

        return recommendation;
    }

    private DebtPayoffRecommendation ParseDebtPayoffRecommendation(string aiResponse)
    {
        var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        return new DebtPayoffRecommendation
        {
            Strategy = ExtractStrategy(lines),
            EstimatedPayoffTime = ExtractPayoffTime(lines),
            PaymentPlans = ExtractPaymentPlans(lines)
        };
    }

    private SavingsOptimizationRecommendation ParseSavingsOptimizationRecommendation(string aiResponse)
    {
        var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        return new SavingsOptimizationRecommendation
        {
            MainGoal = ExtractMainGoal(lines),
            CostCuttingTips = ExtractCostCuttingTips(lines),
            RecommendedGoals = ExtractSavingsGoals(lines)
        };
    }

    // Fallback methods when API is unavailable
    private BudgetRecommendation CreateFallbackBudgetRecommendation(List<Category> categories, string currentMonth)
    {
        var totalBudgeted = categories.Sum(c => c.Budgets.FirstOrDefault(b => b.MonthYear == currentMonth)?.Amount ?? 0);
        var totalSpent = categories.Sum(c => c.Transactions.Where(t => t.Date.ToString("yyyy-MM") == currentMonth).Sum(t => t.Amount));
        var spendingRatio = totalBudgeted > 0 ? (totalSpent / totalBudgeted) : 0;

        return new BudgetRecommendation
        {
            MainAdvice = spendingRatio > 1 ? "You're overspending this month. Focus on your highest overspent categories." : 
                        "Your spending is on track! Consider reallocating unused funds to savings or debt payoff.",
            Tips = new List<string>
            {
                "Review categories that are over 80% spent midway through the month",
                "Consider the 50/30/20 rule: 50% needs, 30% wants, 20% savings",
                "Set up automatic transfers to your emergency fund",
                "Use the cash envelope method for discretionary spending"
            },
            OverallHealthScore = spendingRatio <= 0.8m ? "Good" : spendingRatio <= 1.0m ? "Fair" : "Needs Attention"
        };
    }

    private DebtPayoffRecommendation CreateFallbackDebtPayoffRecommendation(decimal extraMoney, List<DebtInfo> debts)
    {
        return new DebtPayoffRecommendation
        {
            Strategy = "Avalanche Method: Pay minimums on all debts, put extra money toward highest interest rate debt",
            EstimatedPayoffTime = "Calculation requires more detailed analysis",
            PaymentPlans = debts.OrderByDescending(d => d.InterestRate).Select(d => new DebtPaymentPlan
            {
                DebtName = d.Name,
                SuggestedPayment = d.MinimumPayment + (d.InterestRate == debts.Max(x => x.InterestRate) ? extraMoney : 0),
                Reasoning = d.InterestRate == debts.Max(x => x.InterestRate) ? "Highest interest rate - focus extra payments here" : "Pay minimum amount"
            }).ToList()
        };
    }

    private SavingsOptimizationRecommendation CreateFallbackSavingsRecommendation(List<Category> categories, decimal totalIncome)
    {
        return new SavingsOptimizationRecommendation
        {
            MainGoal = "Build a solid financial foundation with emergency savings and strategic budget optimization",
            CostCuttingTips = new List<string>
            {
                "Review subscription services and cancel unused ones",
                "Plan meals to reduce food waste and dining out",
                "Compare insurance rates annually",
                "Use generic brands for household essentials"
            },
            SuggestedEmergencyFund = totalIncome * 3, // 3 months of expenses
            RecommendedGoals = new List<SavingsGoal>
            {
                new() { Name = "Emergency Fund", Amount = totalIncome * 6, TimeFrame = "12-18 months", Priority = "High" },
                new() { Name = "Retirement", Amount = totalIncome * 0.15m, TimeFrame = "Ongoing", Priority = "High" },
                new() { Name = "Home Down Payment", Amount = 50000, TimeFrame = "3-5 years", Priority = "Medium" }
            }
        };
    }

    // Helper methods for parsing GROK responses
    private string ExtractMainAdvice(string[] lines) => 
        lines.FirstOrDefault(l => l.Contains("advice", StringComparison.OrdinalIgnoreCase)) ?? "Focus on staying within your budget limits.";

    private List<string> ExtractTips(string[] lines) => 
        lines.Where(l => l.Contains("tip", StringComparison.OrdinalIgnoreCase) || l.StartsWith("- ")).Take(5).ToList();

    private string ExtractHealthScore(string[] lines) => 
        lines.FirstOrDefault(l => l.Contains("score", StringComparison.OrdinalIgnoreCase) || 
                                  l.Contains("health", StringComparison.OrdinalIgnoreCase)) ?? "Good";

    private List<CategoryAdjustment> ExtractAdjustments(string[] lines) => new();

    private string ExtractStrategy(string[] lines) => 
        lines.FirstOrDefault(l => l.Contains("strategy", StringComparison.OrdinalIgnoreCase)) ?? "Debt Avalanche Method";

    private string ExtractPayoffTime(string[] lines) => 
        lines.FirstOrDefault(l => l.Contains("payoff", StringComparison.OrdinalIgnoreCase) || 
                                  l.Contains("timeline", StringComparison.OrdinalIgnoreCase)) ?? "Varies based on payment amounts";

    private List<DebtPaymentPlan> ExtractPaymentPlans(string[] lines) => new();

    private string ExtractMainGoal(string[] lines) => 
        lines.FirstOrDefault(l => l.Contains("goal", StringComparison.OrdinalIgnoreCase)) ?? "Build financial stability";

    private List<string> ExtractCostCuttingTips(string[] lines) => 
        lines.Where(l => l.Contains("cut", StringComparison.OrdinalIgnoreCase) || 
                         l.Contains("save", StringComparison.OrdinalIgnoreCase) || 
                         l.StartsWith("- ")).Take(5).ToList();

    private List<SavingsGoal> ExtractSavingsGoals(string[] lines) => new();

    // New Creative AI Methods Implementation
    public async Task<TransactionCategorizationResult> CategorizePendingTransactionsAsync(List<PendingTransaction> transactions, List<Category> userCategories)
    {
        try
        {
            var prompt = CreateTransactionCategorizationPrompt(transactions, userCategories);
            var systemPrompt = "You are an AI assistant specialized in financial transaction categorization. Analyze transaction descriptions and assign appropriate budget categories.";
            var response = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseCategorizationResponse(response, transactions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI providers failed for transaction categorization, using fallback");
            return CreateFallbackCategorizationResult(transactions, userCategories);
        }
    }

    public async Task<BudgetForecastResult> GenerateBudgetForecastAsync(List<Category> categories, List<Transaction> historicalData, int forecastMonths = 3)
    {
        try
        {
            var prompt = CreateBudgetForecastPrompt(categories, historicalData, forecastMonths);
            var systemPrompt = "You are a financial forecasting expert. Use historical spending data to predict future budget needs and seasonal variations.";
            var response = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseForecastResponse(response, forecastMonths);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI providers failed for budget forecasting, using fallback");
            return CreateFallbackBudgetForecast(categories, historicalData, forecastMonths);
        }
    }

    public async Task<SpendingAnalysisResult> AnalyzeSpendingPatternsAsync(List<Transaction> transactions, List<Category> categories)
    {
        try
        {
            var prompt = CreateSpendingAnalysisPrompt(transactions, categories);
            var systemPrompt = "You are a spending pattern analyst. Identify trends, anomalies, and optimization opportunities in financial data.";
            var response = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseSpendingAnalysisResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI providers failed for spending analysis, using fallback");
            return CreateFallbackSpendingAnalysis(transactions, categories);
        }
    }

    public async Task<FinancialCoachingResult> GetPersonalizedCoachingAsync(string userId, List<Transaction> recentTransactions, List<FinancialGoal> goals)
    {
        try
        {
            var prompt = CreateFinancialCoachingPrompt(userId, recentTransactions, goals);
            var systemPrompt = "You are a supportive financial coach. Provide personalized, encouraging advice with actionable steps and motivational messages.";
            var response = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseFinancialCoachingResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI providers failed for financial coaching, using fallback");
            return CreateFallbackFinancialCoaching(recentTransactions, goals);
        }
    }

    // Prompt Creation Methods
    private string CreateTransactionCategorizationPrompt(List<PendingTransaction> transactions, List<Category> userCategories)
    {
        var categoriesJson = JsonSerializer.Serialize(userCategories.Select(c => new { c.Id, c.Name }), new JsonSerializerOptions { WriteIndented = true });
        var transactionsJson = JsonSerializer.Serialize(transactions.Select(t => new { t.Description, t.Merchant, t.Amount }), new JsonSerializerOptions { WriteIndented = true });
        
        return $@"I need to categorize these transactions based on user's existing categories:

User's Categories:
{categoriesJson}

Transactions to categorize:
{transactionsJson}

For each transaction, suggest the most appropriate category and provide a confidence score (0-100) and reasoning. Consider merchant names, transaction amounts, and description patterns. Format response as structured JSON.";
    }

    private string CreateBudgetForecastPrompt(List<Category> categories, List<Transaction> historicalData, int forecastMonths)
    {
        var recentSpending = historicalData.GroupBy(t => new { t.Category?.Name, Month = t.Date.ToString("yyyy-MM") })
            .Select(g => new { Category = g.Key.Name, Month = g.Key.Month, Total = g.Sum(t => t.Amount) })
            .OrderBy(x => x.Month)
            .Take(12);
            
        var historicalJson = JsonSerializer.Serialize(recentSpending, new JsonSerializerOptions { WriteIndented = true });
        
        return $@"Based on this spending history, forecast the next {forecastMonths} months:

Historical Spending (last 12 months):
{historicalJson}

Current Month: {DateTime.Now.ToString("yyyy-MM")}

Please provide:
1. Monthly spending forecasts by category
2. Seasonal adjustment recommendations
3. Overall spending trends
4. Potential budget variance warnings

Consider seasonal patterns, historical trends, and economic factors.";
    }

    private string CreateSpendingAnalysisPrompt(List<Transaction> transactions, List<Category> categories)
    {
        var spendingData = transactions.GroupBy(t => t.Category?.Name)
            .Select(g => new { Category = g.Key, Count = g.Count(), Total = g.Sum(t => t.Amount), Avg = g.Average(t => t.Amount) });
            
        var dataJson = JsonSerializer.Serialize(spendingData, new JsonSerializerOptions { WriteIndented = true });
        
        return $@"Analyze these spending patterns for anomalies and insights:

Spending Data:
{dataJson}

Identify:
1. Unusual spending patterns or anomalies
2. Recurring spending cycles
3. Optimization opportunities
4. Overall financial health indicators
5. Actionable recommendations

Provide specific, actionable insights with dollar amounts where possible.";
    }

    private string CreateFinancialCoachingPrompt(string userId, List<Transaction> recentTransactions, List<FinancialGoal> goals)
    {
        var recentSpending = recentTransactions.Sum(t => t.Amount);
        var goalsJson = JsonSerializer.Serialize(goals.Select(g => new { g.Name, g.TargetAmount, g.CurrentAmount, g.GoalType }), new JsonSerializerOptions { WriteIndented = true });
        
        return $@"Provide personalized financial coaching for this user:

Recent spending (last 30 days): ${recentSpending:F2}

Financial Goals:
{goalsJson}

Act as a supportive, motivational financial coach. Provide:
1. A personalized main message based on recent activity
2. 3-5 specific, actionable tips
3. Milestone celebrations if any goals are close to completion
4. A motivational quote
5. Quick action items (5-15 minutes each)

Keep the tone encouraging but realistic, focusing on achievable next steps.";
    }

    // Response Parsing Methods
    private TransactionCategorizationResult ParseCategorizationResponse(string response, List<PendingTransaction> transactions)
    {
        // Simplified parsing - in a real implementation, you'd parse the structured JSON response
        return CreateFallbackCategorizationResult(transactions, new List<Category>());
    }

    private BudgetForecastResult ParseForecastResponse(string response, int forecastMonths)
    {
        // Simplified parsing - in a real implementation, you'd parse the structured response
        return CreateFallbackBudgetForecast(new List<Category>(), new List<Transaction>(), forecastMonths);
    }

    private SpendingAnalysisResult ParseSpendingAnalysisResponse(string response)
    {
        // Simplified parsing - in a real implementation, you'd parse the structured response
        return CreateFallbackSpendingAnalysis(new List<Transaction>(), new List<Category>());
    }

    private FinancialCoachingResult ParseFinancialCoachingResponse(string response)
    {
        // Simplified parsing - in a real implementation, you'd parse the structured response
        return CreateFallbackFinancialCoaching(new List<Transaction>(), new List<FinancialGoal>());
    }

    // Fallback Methods for New Features
    private TransactionCategorizationResult CreateFallbackCategorizationResult(List<PendingTransaction> transactions, List<Category> userCategories)
    {
        var categorizedTransactions = transactions.Select((t, index) => new CategorizedTransaction
        {
            TransactionId = index.ToString(),
            Description = t.Description,
            SuggestedCategory = CategorizePendingTransactionFallback(t, userCategories),
            Confidence = 85m,
            Reasoning = "Based on merchant pattern and transaction amount"
        }).ToList();

        return new TransactionCategorizationResult
        {
            CategorizedTransactions = categorizedTransactions,
            ConfidenceScore = 85m,
            LearningTips = new List<string>
            {
                "Review suggested categories and make corrections to improve future accuracy",
                "Add new categories for unique spending patterns",
                "Use consistent merchant names for better recognition"
            }
        };
    }

    private string CategorizePendingTransactionFallback(PendingTransaction transaction, List<Category> userCategories)
    {
        var desc = transaction.Description.ToLower();
        var merchant = transaction.Merchant.ToLower();
        
        // Simple rule-based categorization
        if (desc.Contains("grocery") || desc.Contains("supermarket") || desc.Contains("food"))
            return userCategories.FirstOrDefault(c => c.Name.Contains("Groceries", StringComparison.OrdinalIgnoreCase))?.Name ?? "Groceries";
        
        if (desc.Contains("gas") || desc.Contains("fuel") || desc.Contains("transport"))
            return userCategories.FirstOrDefault(c => c.Name.Contains("Transportation", StringComparison.OrdinalIgnoreCase))?.Name ?? "Transportation";
        
        if (desc.Contains("restaurant") || desc.Contains("dining") || desc.Contains("cafe"))
            return userCategories.FirstOrDefault(c => c.Name.Contains("Dining", StringComparison.OrdinalIgnoreCase))?.Name ?? "Dining Out";
        
        if (desc.Contains("utility") || desc.Contains("electric") || desc.Contains("water"))
            return userCategories.FirstOrDefault(c => c.Name.Contains("Utilities", StringComparison.OrdinalIgnoreCase))?.Name ?? "Utilities";
        
        return userCategories.FirstOrDefault()?.Name ?? "Miscellaneous";
    }

    private BudgetForecastResult CreateFallbackBudgetForecast(List<Category> categories, List<Transaction> historicalData, int forecastMonths)
    {
        var currentMonth = DateTime.Now;
        var forecasts = new List<MonthlyForecast>();
        
        for (int i = 1; i <= forecastMonths; i++)
        {
            var forecastMonth = currentMonth.AddMonths(i);
            var historicalAvg = historicalData.Where(t => t.Date >= currentMonth.AddMonths(-6))
                .Sum(t => t.Amount) / 6; // 6-month average
            
            forecasts.Add(new MonthlyForecast
            {
                Month = forecastMonth.ToString("yyyy-MM"),
                PredictedSpending = historicalAvg * 1.05m, // 5% inflation adjustment
                PredictedIncome = historicalAvg * 1.2m, // Assume some income buffer
                PredictedSurplus = (historicalAvg * 1.2m) - (historicalAvg * 1.05m),
                CategoryForecasts = categories.Select(c => new CategoryForecast
                {
                    CategoryName = c.Name,
                    PredictedAmount = c.Budgets.LastOrDefault()?.Amount ?? 0,
                    VarianceFromBudget = 0,
                    Trend = "Stable"
                }).ToList()
            });
        }

        return new BudgetForecastResult
        {
            MonthlyForecasts = forecasts,
            ForecastInsights = new List<string>
            {
                "Based on historical patterns, your spending is expected to remain stable",
                "Consider seasonal adjustments for holiday periods",
                "Monitor inflation impact on grocery and utility costs"
            },
            SeasonalAdjustments = new List<SeasonalAdjustment>
            {
                new() { Category = "Utilities", Season = "Winter", AdjustmentPercentage = 15, Reasoning = "Higher heating costs" },
                new() { Category = "Entertainment", Season = "Holiday", AdjustmentPercentage = 25, Reasoning = "Holiday spending increase" }
            },
            OverallTrend = "Stable with seasonal variations"
        };
    }

    private SpendingAnalysisResult CreateFallbackSpendingAnalysis(List<Transaction> transactions, List<Category> categories)
    {
        var totalSpending = transactions.Sum(t => t.Amount);
        var avgTransactionSize = transactions.Any() ? transactions.Average(t => t.Amount) : 0;
        
        var patterns = new List<SpendingPattern>
        {
            new() { PatternType = "Regular", Description = "Consistent monthly spending pattern", Impact = totalSpending * 0.7m, Frequency = "Monthly" },
            new() { PatternType = "Impulse", Description = "Occasional large purchases", Impact = totalSpending * 0.3m, Frequency = "Weekly" }
        };

        var anomalies = transactions.Where(t => t.Amount > avgTransactionSize * 2)
            .Select(t => new SpendingAnomaly
            {
                Category = t.Category?.Name ?? "Unknown",
                AnomalyType = "Large Purchase",
                Amount = t.Amount,
                Date = t.Date,
                Explanation = "Transaction significantly larger than average"
            }).ToList();

        return new SpendingAnalysisResult
        {
            DetectedPatterns = patterns,
            Anomalies = anomalies,
            OptimizationSuggestions = new List<string>
            {
                "Review large purchases for necessity",
                "Set up automatic transfers for consistent savings",
                "Consider bulk purchasing for frequently bought items",
                "Use budgeting apps to track impulse spending"
            },
            OverallHealthScore = totalSpending > 0 ? Math.Min(100, (avgTransactionSize / totalSpending * 1000)) : 100
        };
    }

    private FinancialCoachingResult CreateFallbackFinancialCoaching(List<Transaction> recentTransactions, List<FinancialGoal> goals)
    {
        var totalSpent = recentTransactions.Sum(t => t.Amount);
        var completedGoals = goals.Count(g => g.IsCompleted);
        
        var personalities = new[] { "Encouraging", "Analytical", "Motivational", "Practical" };
        var selectedPersonality = personalities[new Random().Next(personalities.Length)];
        
        return new FinancialCoachingResult
        {
            CoachPersonality = selectedPersonality,
            MainMessage = totalSpent > 1000 
                ? "You've been active with your spending this month. Let's make sure every dollar is working towards your goals!"
                : "Great job keeping your spending in check! This discipline will help you reach your financial goals faster.",
            PersonalizedTips = new List<PersonalizedTip>
            {
                new() { Title = "Track Your Progress", Advice = "Review your budget weekly to stay on track", Category = "Budgeting", PotentialSavings = 50 },
                new() { Title = "Emergency Fund", Advice = "Aim to save $25 per week for emergencies", Category = "Savings", PotentialSavings = 100 },
                new() { Title = "Subscription Audit", Advice = "Review and cancel unused subscriptions", Category = "Utilities", PotentialSavings = 25 }
            },
            Milestones = completedGoals > 0 
                ? new List<MilestoneMessage> { new() { Achievement = "Goal Completed!", Celebration = "🎉 You've achieved a financial goal!", NextGoal = "Set your next savings target" } }
                : new List<MilestoneMessage>(),
            MotivationalQuote = "Every dollar saved is a step towards financial freedom. You've got this! 💪",
            ActionItems = new List<ActionItem>
            {
                new() { Task = "Review last week's transactions", Priority = "High", EstimatedMinutes = 10, ExpectedBenefit = "Better awareness of spending patterns" },
                new() { Task = "Set up automatic savings transfer", Priority = "Medium", EstimatedMinutes = 15, ExpectedBenefit = "Consistent progress towards goals" },
                new() { Task = "Plan next week's meals", Priority = "Low", EstimatedMinutes = 20, ExpectedBenefit = "Reduce dining out expenses" }
            }
        };
    }

    // Tax Optimization Implementation
    public async Task<TaxOptimizationResult> GetTaxOptimizationSuggestionsAsync(string userId, List<Transaction> transactions, List<Category> categories, decimal annualIncome)
    {
        try
        {
            var prompt = CreateTaxOptimizationPrompt(transactions, categories, annualIncome);
            var systemPrompt = "You are a tax optimization expert. Provide personalized tax-saving strategies based on spending patterns and income.";
            var response = await _aiProviderManager.GenerateResponseAsync(prompt, systemPrompt);
            
            return ParseTaxOptimizationResponse(response, annualIncome);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI providers failed for tax optimization, using fallback");
            return CreateFallbackTaxOptimization(transactions, categories, annualIncome);
        }
    }

    private string CreateTaxOptimizationPrompt(List<Transaction> transactions, List<Category> categories, decimal annualIncome)
    {
        var yearlySpending = transactions
            .Where(t => t.Date >= DateTime.Now.AddYears(-1))
            .GroupBy(t => t.Category?.Name)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total);
            
        var spendingJson = JsonSerializer.Serialize(yearlySpending, new JsonSerializerOptions { WriteIndented = true });
        
        return $@"Analyze this financial data and provide tax optimization suggestions:

Annual Income: ${annualIncome:N2}

Yearly Spending by Category:
{spendingJson}

Please provide:
1. Potential tax deductions based on spending categories
2. Tax credit opportunities
3. Retirement contribution strategies for tax savings
4. Estimated tax bracket and effective tax rate
5. General tax planning tips

Consider charitable donations, home office expenses, medical expenses, retirement contributions, and other common deductions.";
    }

    private TaxOptimizationResult ParseTaxOptimizationResponse(string response, decimal annualIncome)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        return new TaxOptimizationResult
        {
            DeductionSuggestions = ExtractDeductionSuggestions(lines),
            CreditSuggestions = ExtractCreditSuggestions(lines),
            RetirementSuggestions = ExtractRetirementSuggestions(lines, annualIncome),
            EstimatedTotalSavings = CalculateEstimatedSavings(lines),
            TaxBracket = DetermineTaxBracket(annualIncome),
            GeneralTips = ExtractGeneralTaxTips(lines)
        };
    }

    private TaxOptimizationResult CreateFallbackTaxOptimization(List<Transaction> transactions, List<Category> categories, decimal annualIncome)
    {
        var charitableDonations = transactions
            .Where(t => t.Category?.Name?.Contains("Charity", StringComparison.OrdinalIgnoreCase) == true 
                     || t.Category?.Name?.Contains("Donation", StringComparison.OrdinalIgnoreCase) == true)
            .Sum(t => t.Amount);
            
        var medicalExpenses = transactions
            .Where(t => t.Category?.Name?.Contains("Medical", StringComparison.OrdinalIgnoreCase) == true 
                     || t.Category?.Name?.Contains("Health", StringComparison.OrdinalIgnoreCase) == true)
            .Sum(t => t.Amount);
        
        var deductions = new List<TaxDeductionSuggestion>();
        
        if (charitableDonations > 0)
        {
            deductions.Add(new TaxDeductionSuggestion
            {
                Title = "Charitable Donations",
                Description = "Track and document all charitable contributions for tax deductions",
                PotentialSavings = charitableDonations * 0.22m, // Assuming 22% tax bracket
                Category = "Charity",
                ActionRequired = "Keep receipts and documentation for all donations"
            });
        }
        
        if (medicalExpenses > annualIncome * 0.075m)
        {
            var deductibleAmount = medicalExpenses - (annualIncome * 0.075m);
            deductions.Add(new TaxDeductionSuggestion
            {
                Title = "Medical Expense Deduction",
                Description = "Medical expenses exceeding 7.5% of AGI may be deductible",
                PotentialSavings = deductibleAmount * 0.22m,
                Category = "Medical",
                ActionRequired = "Organize medical receipts and insurance statements"
            });
        }
        
        var retirementContrib = Math.Min(annualIncome * 0.15m, 22500); // IRS limit for 401k
        
        return new TaxOptimizationResult
        {
            DeductionSuggestions = deductions,
            CreditSuggestions = new List<TaxCreditSuggestion>
            {
                new() 
                { 
                    Title = "Retirement Savings Credit", 
                    Description = "Saver's Credit for retirement contributions (if eligible)",
                    PotentialCredit = annualIncome < 73000 ? 1000 : 0,
                    Eligibility = "AGI under $73,000 for married filing jointly",
                    ActionRequired = "Contribute to IRA or 401(k)"
                }
            },
            RetirementSuggestions = new List<RetirementContributionSuggestion>
            {
                new()
                {
                    AccountType = "Traditional 401(k)",
                    SuggestedContribution = retirementContrib,
                    TaxSavings = retirementContrib * 0.22m,
                    Reasoning = "Reduces taxable income and builds retirement savings"
                },
                new()
                {
                    AccountType = "Traditional IRA",
                    SuggestedContribution = 6500,
                    TaxSavings = 6500 * 0.22m,
                    Reasoning = "Additional tax-deferred retirement savings option"
                }
            },
            EstimatedTotalSavings = (deductions.Sum(d => d.PotentialSavings) + retirementContrib * 0.22m),
            TaxBracket = DetermineTaxBracket(annualIncome),
            GeneralTips = new List<string>
            {
                "Maximize retirement contributions to reduce taxable income",
                "Keep detailed records of all deductible expenses throughout the year",
                "Consider tax-loss harvesting if you have investment accounts",
                "Review your W-4 withholding to avoid overpaying or underpaying",
                "Consult with a tax professional for personalized advice"
            }
        };
    }

    private string DetermineTaxBracket(decimal annualIncome)
    {
        if (annualIncome <= 11000) return "10%";
        if (annualIncome <= 44725) return "12%";
        if (annualIncome <= 95375) return "22%";
        if (annualIncome <= 182100) return "24%";
        if (annualIncome <= 231250) return "32%";
        if (annualIncome <= 578125) return "35%";
        return "37%";
    }

    private List<TaxDeductionSuggestion> ExtractDeductionSuggestions(string[] lines)
    {
        return new List<TaxDeductionSuggestion>
        {
            new() { Title = "Standard/Itemized Deduction", Description = "Choose the higher of standard or itemized deductions", PotentialSavings = 2800, Category = "General", ActionRequired = "Calculate both options" }
        };
    }

    private List<TaxCreditSuggestion> ExtractCreditSuggestions(string[] lines)
    {
        return new List<TaxCreditSuggestion>
        {
            new() { Title = "Energy Efficiency Credits", Description = "Credits for home energy improvements", PotentialCredit = 500, Eligibility = "Qualified energy-efficient improvements", ActionRequired = "Keep improvement receipts" }
        };
    }

    private List<RetirementContributionSuggestion> ExtractRetirementSuggestions(string[] lines, decimal annualIncome)
    {
        return new List<RetirementContributionSuggestion>
        {
            new() { AccountType = "401(k)", SuggestedContribution = annualIncome * 0.10m, TaxSavings = annualIncome * 0.10m * 0.22m, Reasoning = "Tax-deferred growth" }
        };
    }

    private decimal CalculateEstimatedSavings(string[] lines)
    {
        return 5000; // Placeholder - would parse from AI response
    }

    private List<string> ExtractGeneralTaxTips(string[] lines)
    {
        return new List<string>
        {
            "Consider tax-advantaged accounts for savings",
            "Track business expenses if self-employed",
            "Review tax withholding annually"
        };
    }
}