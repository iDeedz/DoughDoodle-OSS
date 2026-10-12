namespace DoughDoodle.Services.Billing;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Domain { get; set; } = "https://localhost:5001";

    public StripePriceCatalog Prices { get; set; } = new();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(PublishableKey)
        && !string.IsNullOrWhiteSpace(Domain)
        && Prices.HasAllMonthlyPrices;

    public bool CanVerifyWebhooks =>
        !string.IsNullOrWhiteSpace(SecretKey) && !string.IsNullOrWhiteSpace(WebhookSecret);

    public IEnumerable<(string Key, string? Monthly, string? Yearly)> EnumeratePricePairs()
    {
        yield return ("Bankroll", Prices.BankrollMonthly, Prices.BankrollYearly);
        yield return ("Stack", Prices.StackMonthly, Prices.StackYearly);
        yield return ("Vault", Prices.VaultMonthly, Prices.VaultYearly);
        yield return ("Prime", Prices.PrimeMonthly, Prices.PrimeYearly);
    }

    public string? ResolveMonthlyPriceId(string packKey) => packKey switch
    {
        "Bankroll" => Prices.BankrollMonthly,
        "Stack" => Prices.StackMonthly,
        "Vault" => Prices.VaultMonthly,
        "Prime" => Prices.PrimeMonthly,
        _ => null
    };

    public string? ResolveYearlyPriceId(string packKey) => packKey switch
    {
        "Bankroll" => Prices.BankrollYearly,
        "Stack" => Prices.StackYearly,
        "Vault" => Prices.VaultYearly,
        "Prime" => Prices.PrimeYearly,
        _ => null
    };

    public bool IsKnownPriceId(string? priceId)
    {
        if (string.IsNullOrWhiteSpace(priceId)) return false;
        foreach (var (_, monthly, yearly) in EnumeratePricePairs())
        {
            if (string.Equals(priceId, monthly, StringComparison.Ordinal)
                || string.Equals(priceId, yearly, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}

public class StripePriceCatalog
{
    public string BankrollMonthly { get; set; } = string.Empty;
    public string BankrollYearly { get; set; } = string.Empty;
    public string StackMonthly { get; set; } = string.Empty;
    public string StackYearly { get; set; } = string.Empty;
    public string VaultMonthly { get; set; } = string.Empty;
    public string VaultYearly { get; set; } = string.Empty;
    public string PrimeMonthly { get; set; } = string.Empty;
    public string PrimeYearly { get; set; } = string.Empty;

    public bool HasAllMonthlyPrices =>
        !string.IsNullOrWhiteSpace(BankrollMonthly)
        && !string.IsNullOrWhiteSpace(StackMonthly)
        && !string.IsNullOrWhiteSpace(VaultMonthly)
        && !string.IsNullOrWhiteSpace(PrimeMonthly);
}
