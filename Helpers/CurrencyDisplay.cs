using System.Globalization;

namespace DoughDoodle.Helpers;

/// <summary>
/// Display-only currency formatting using the user's PreferredCurrency label.
/// Does not claim live FX conversion.
/// </summary>
public static class CurrencyDisplay
{
    public static string Normalize(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return "USD";
        return currencyCode.Trim().ToUpperInvariant();
    }

    /// <summary>Currency symbol for input adornments and chart tick prefixes (never mystery ¤).</summary>
    public static string Symbol(string? currencyCode = null)
    {
        var code = Normalize(currencyCode);
        try
        {
            var symbol = CultureFor(code).NumberFormat.CurrencySymbol;
            if (string.IsNullOrWhiteSpace(symbol) || symbol == "¤")
                return code;
            return symbol;
        }
        catch (CultureNotFoundException)
        {
            return code;
        }
    }

    public static string Format(decimal amount, string? currencyCode = null, bool compact = false)
    {
        var code = Normalize(currencyCode);

        try
        {
            var culture = CultureFor(code);
            return amount.ToString(compact ? "C0" : "C", culture);
        }
        catch (CultureNotFoundException)
        {
            return compact
                ? $"{code} {amount:N0}"
                : $"{code} {amount:N2}";
        }
    }

    private static CultureInfo CultureFor(string code) => code switch
    {
        "ZAR" => new CultureInfo("en-ZA"),
        "EUR" => new CultureInfo("de-DE"),
        "GBP" => new CultureInfo("en-GB"),
        "AUD" => new CultureInfo("en-AU"),
        "CAD" => new CultureInfo("en-CA"),
        "JPY" => new CultureInfo("ja-JP"),
        _ => new CultureInfo("en-US")
    };
}
