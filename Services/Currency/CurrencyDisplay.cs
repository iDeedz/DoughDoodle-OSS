using System.Globalization;

namespace DoughDoodle.Services.Currency;

/// <summary>
/// Display-only ISO currency labels. Does not convert amounts and does not claim live FX.
/// Symbols come from <see cref="NumberFormatInfo"/> for a mapped culture.
/// </summary>
public static class CurrencyDisplay
{
    private static readonly Dictionary<string, string> CultureByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "en-US",
        ["EUR"] = "de-DE",
        ["GBP"] = "en-GB",
        ["ZAR"] = "en-ZA",
        ["AUD"] = "en-AU",
        ["CAD"] = "en-CA"
    };

    public static string NormalizeCode(string? preferredCurrency)
    {
        if (string.IsNullOrWhiteSpace(preferredCurrency))
            return "USD";

        var code = preferredCurrency.Trim().ToUpperInvariant();
        return code.Length == 3 && code.All(char.IsAsciiLetter) ? code : "USD";
    }

    public static string Symbol(string? preferredCurrency)
    {
        var code = NormalizeCode(preferredCurrency);
        if (!CultureByCode.TryGetValue(code, out var cultureName))
            return code + " ";

        var symbol = CultureInfo.GetCultureInfo(cultureName).NumberFormat.CurrencySymbol;
        // en-AU / en-CA NumberFormatInfo is also "$"; keep an ISO-visible prefix so they are not USD.
        return code switch
        {
            "AUD" when symbol == "$" => "A$",
            "CAD" when symbol == "$" => "C$",
            _ => symbol
        };
    }

    public static string Format(decimal amount, string? preferredCurrency, string numericFormat = "N2")
    {
        var number = amount.ToString(numericFormat, CultureInfo.InvariantCulture);
        return Symbol(preferredCurrency) + number;
    }
}
