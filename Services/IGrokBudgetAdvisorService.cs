using DoughDoodle.Models;

namespace DoughDoodle.Services;

public interface IGrokBudgetAdvisorService
{
    Task<BudgetRecommendation> GetBudgetRecommendationsAsync(List<Category> categories, string currentMonth);
    Task<DebtPayoffRecommendation> GetDebtPayoffRecommendationsAsync(decimal extraMoney, List<DebtInfo> debts);
    Task<SavingsOptimizationRecommendation> GetSavingsOptimizationAsync(List<Category> categories, decimal totalIncome);
    
    // New Creative AI Features
    Task<TransactionCategorizationResult> CategorizePendingTransactionsAsync(List<PendingTransaction> transactions, List<Category> userCategories);
    Task<BudgetForecastResult> GenerateBudgetForecastAsync(List<Category> categories, List<Transaction> historicalData, int forecastMonths = 3);
    Task<SpendingAnalysisResult> AnalyzeSpendingPatternsAsync(List<Transaction> transactions, List<Category> categories);
    Task<FinancialCoachingResult> GetPersonalizedCoachingAsync(string userId, List<Transaction> recentTransactions, List<FinancialGoal> goals);
    
    // Tax optimization
    Task<TaxOptimizationResult> GetTaxOptimizationSuggestionsAsync(string userId, List<Transaction> transactions, List<Category> categories, decimal annualIncome);
}

public class BudgetRecommendation
{
    public string MainAdvice { get; set; } = string.Empty;
    public List<string> Tips { get; set; } = new();
    public List<CategoryAdjustment> SuggestedAdjustments { get; set; } = new();
    public string OverallHealthScore { get; set; } = string.Empty;
}

public class CategoryAdjustment
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentBudget { get; set; }
    public decimal SuggestedBudget { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class DebtPayoffRecommendation
{
    public string Strategy { get; set; } = string.Empty;
    public List<DebtPaymentPlan> PaymentPlans { get; set; } = new();
    public string EstimatedPayoffTime { get; set; } = string.Empty;
    public decimal TotalInterestSaved { get; set; }
}

public class DebtPaymentPlan
{
    public string DebtName { get; set; } = string.Empty;
    public decimal SuggestedPayment { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public class DebtInfo
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MinimumPayment { get; set; }
}

public class SavingsOptimizationRecommendation
{
    public string MainGoal { get; set; } = string.Empty;
    public List<string> CostCuttingTips { get; set; } = new();
    public decimal SuggestedEmergencyFund { get; set; }
    public List<SavingsGoal> RecommendedGoals { get; set; } = new();
}

public class SavingsGoal
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}

// New Creative AI Models
public class PendingTransaction
{
    public string Description { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? SuggestedCategoryId { get; set; }
}

public class TransactionCategorizationResult
{
    public List<CategorizedTransaction> CategorizedTransactions { get; set; } = new();
    public decimal ConfidenceScore { get; set; }
    public List<string> LearningTips { get; set; } = new();
}

public class CategorizedTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SuggestedCategory { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public class BudgetForecastResult
{
    public List<MonthlyForecast> MonthlyForecasts { get; set; } = new();
    public List<string> ForecastInsights { get; set; } = new();
    public List<SeasonalAdjustment> SeasonalAdjustments { get; set; } = new();
    public string OverallTrend { get; set; } = string.Empty;
}

public class MonthlyForecast
{
    public string Month { get; set; } = string.Empty;
    public decimal PredictedSpending { get; set; }
    public decimal PredictedIncome { get; set; }
    public decimal PredictedSurplus { get; set; }
    public List<CategoryForecast> CategoryForecasts { get; set; } = new();
}

public class CategoryForecast
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal PredictedAmount { get; set; }
    public decimal VarianceFromBudget { get; set; }
    public string Trend { get; set; } = string.Empty;
}

public class SeasonalAdjustment
{
    public string Category { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public decimal AdjustmentPercentage { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public class SpendingAnalysisResult
{
    public List<SpendingPattern> DetectedPatterns { get; set; } = new();
    public List<SpendingAnomaly> Anomalies { get; set; } = new();
    public List<string> OptimizationSuggestions { get; set; } = new();
    public decimal OverallHealthScore { get; set; }
}

public class SpendingPattern
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public string Frequency { get; set; } = string.Empty;
}

public class SpendingAnomaly
{
    public string Category { get; set; } = string.Empty;
    public string AnomalyType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class FinancialCoachingResult
{
    public string CoachPersonality { get; set; } = "Supportive";
    public string MainMessage { get; set; } = string.Empty;
    public List<PersonalizedTip> PersonalizedTips { get; set; } = new();
    public List<MilestoneMessage> Milestones { get; set; } = new();
    public string MotivationalQuote { get; set; } = string.Empty;
    public List<ActionItem> ActionItems { get; set; } = new();
}

public class PersonalizedTip
{
    public string Title { get; set; } = string.Empty;
    public string Advice { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PotentialSavings { get; set; }
}

public class MilestoneMessage
{
    public string Achievement { get; set; } = string.Empty;
    public string Celebration { get; set; } = string.Empty;
    public string NextGoal { get; set; } = string.Empty;
}

public class ActionItem
{
    public string Task { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public string ExpectedBenefit { get; set; } = string.Empty;
}

public class TaxOptimizationResult
{
    public List<TaxDeductionSuggestion> DeductionSuggestions { get; set; } = new();
    public List<TaxCreditSuggestion> CreditSuggestions { get; set; } = new();
    public List<RetirementContributionSuggestion> RetirementSuggestions { get; set; } = new();
    public decimal EstimatedTotalSavings { get; set; }
    public string TaxBracket { get; set; } = string.Empty;
    public List<string> GeneralTips { get; set; } = new();
}

public class TaxDeductionSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialSavings { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;
}

public class TaxCreditSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialCredit { get; set; }
    public string Eligibility { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;
}

public class RetirementContributionSuggestion
{
    public string AccountType { get; set; } = string.Empty;
    public decimal SuggestedContribution { get; set; }
    public decimal TaxSavings { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}